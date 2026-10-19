using System.Text.Json;
using Aquari.One.Auth;
using Aquari.One.Oidc;
using Aquari.One.Transport;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Aquari.One.Internal;

internal static class AquariOAuthProtocol
{
    private const string CanonicalHandleClaim =
        "https://aquari.one/claims/canonical_handle";

    private const string VerifiedHandleClaim =
        "https://aquari.one/claims/verified_handle";

    private static readonly TimeSpan ClockSkew =
        TimeSpan.FromSeconds(60);

    internal static async Task<AquariCompleteAuthorizationResult>
        ExchangeAuthorizationCodeAsync(
            AquariOAuthHttpClient http,
            AquariOidcDiscovery discovery,
            string clientId,
            string? clientSecret,
            Uri redirectUri,
            string code,
            string codeVerifier,
            IReadOnlyList<string> requestedScopes,
            string expectedNonce,
            CancellationToken cancellationToken)
    {
        RequireClientAuthentication(discovery, clientSecret);

        var parameters =
            new List<KeyValuePair<string, string>>
            {
                new("grant_type", "authorization_code"),
                new("client_id", clientId),
                new("redirect_uri", redirectUri.AbsoluteUri),
                new("code", code),
                new("code_verifier", codeVerifier),
            };

        if (clientSecret is not null)
        {
            parameters.Add(
                new KeyValuePair<string, string>(
                    "client_secret",
                    clientSecret));
        }

        using var response =
            await http.PostFormAsync(
                discovery.TokenEndpoint,
                parameters,
                "token_exchange_failed",
                "token exchange",
                cancellationToken).ConfigureAwait(false);

        var parsed =
            ParseAuthorizationTokenResponse(
                response.RootElement,
                requestedScopes);

        var identity =
            await ValidateIdTokenAsync(
                http,
                parsed.IdToken,
                discovery,
                clientId,
                expectedNonce,
                cancellationToken).ConfigureAwait(false);

        return new AquariCompleteAuthorizationResult
        {
            Tokens =
                new AquariTokenSet
                {
                    Subject = identity.Subject,
                    AccessToken = parsed.AccessToken,
                    IdToken = parsed.IdToken,
                    RefreshToken = parsed.RefreshToken,
                    ExpiresIn = parsed.ExpiresIn,
                    ExpiresAt = parsed.ExpiresAt,
                    Scopes = parsed.Scopes,
                },
            Identity = identity,
        };
    }

    internal static async Task<AquariTokenSet> RefreshAsync(
        AquariOAuthHttpClient http,
        AquariOidcDiscovery discovery,
        string clientId,
        string? clientSecret,
        AquariTokenSet currentTokens,
        CancellationToken cancellationToken)
    {
        ValidateTokenSet(currentTokens);
        RequireClientAuthentication(discovery, clientSecret);

        if (currentTokens.RefreshToken is null)
        {
            throw new AquariOAuthException(
                "missing_refresh_token",
                "Aquari cannot refresh this token set because it has no refresh token.");
        }

        var parameters =
            new List<KeyValuePair<string, string>>
            {
                new("grant_type", "refresh_token"),
                new("client_id", clientId),
                new("refresh_token", currentTokens.RefreshToken),
            };

        if (clientSecret is not null)
        {
            parameters.Add(
                new KeyValuePair<string, string>(
                    "client_secret",
                    clientSecret));
        }

        using var response =
            await http.PostFormAsync(
                discovery.TokenEndpoint,
                parameters,
                "refresh_failed",
                "token refresh",
                cancellationToken).ConfigureAwait(false);

        var refreshed =
            ParseRefreshTokenResponse(
                response.RootElement,
                currentTokens);

        if (!string.Equals(
                refreshed.IdToken,
                currentTokens.IdToken,
                StringComparison.Ordinal))
        {
            var identity =
                await ValidateIdTokenAsync(
                    http,
                    refreshed.IdToken,
                    discovery,
                    clientId,
                    expectedNonce: null,
                    cancellationToken).ConfigureAwait(false);

            if (!string.Equals(
                    identity.Subject,
                    currentTokens.Subject,
                    StringComparison.Ordinal))
            {
                throw new AquariOAuthException(
                    "subject_mismatch",
                    "Aquari refreshed ID token subject does not match the existing token set.");
            }
        }

        return refreshed;
    }

    internal static async Task<AquariIdentity> UserInfoAsync(
        AquariOAuthHttpClient http,
        AquariOidcDiscovery discovery,
        AquariTokenSet tokens,
        CancellationToken cancellationToken)
    {
        ValidateTokenSet(tokens);

        using var response =
            await http.GetJsonAsync(
                discovery.UserInfoEndpoint,
                tokens.AccessToken,
                "userinfo_failed",
                "UserInfo request",
                cancellationToken).ConfigureAwait(false);

        var identity =
            ParseIdentity(
                response.RootElement,
                "invalid_userinfo_response");

        if (!string.Equals(
                identity.Subject,
                tokens.Subject,
                StringComparison.Ordinal))
        {
            throw new AquariOAuthException(
                "userinfo_subject_mismatch",
                "Aquari UserInfo subject does not match the authenticated token set.");
        }

        return identity;
    }

    private static void RequireClientAuthentication(
        AquariOidcDiscovery discovery,
        string? clientSecret)
    {
        if (clientSecret is not null &&
            !discovery.TokenEndpointAuthMethodsSupported.Contains(
                "client_secret_post",
                StringComparer.Ordinal))
        {
            throw new AquariOAuthException(
                "unsupported_client_authentication",
                "Aquari discovery does not advertise client_secret_post.");
        }
    }

    private static async Task<AquariIdentity> ValidateIdTokenAsync(
        AquariOAuthHttpClient http,
        string idToken,
        AquariOidcDiscovery discovery,
        string clientId,
        string? expectedNonce,
        CancellationToken cancellationToken)
    {
        string jwksJson;

        try
        {
            jwksJson =
                await http.GetTextAsync(
                    discovery.JwksUri,
                    "invalid_id_token",
                    "JWKS request",
                    cancellationToken).ConfigureAwait(false);
        }
        catch (AquariTransportException)
        {
            throw;
        }
        catch (AquariOAuthException)
        {
            throw InvalidIdToken();
        }

        TokenValidationResult validation;

        try
        {
            var keySet =
                new JsonWebKeySet(jwksJson);

            var parameters =
                new TokenValidationParameters
                {
                    ValidIssuer =
                        discovery.Issuer.AbsoluteUri,
                    ValidAudience = clientId,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKeys = keySet.Keys,
                    ClockSkew = ClockSkew,
                    ValidAlgorithms =
                        new[]
                        {
                            SecurityAlgorithms.RsaSha256,
                        },
                };

            validation =
                await new JsonWebTokenHandler()
                    .ValidateTokenAsync(
                        idToken,
                        parameters)
                    .ConfigureAwait(false);
        }
        catch (ArgumentException)
        {
            throw InvalidIdToken();
        }
        catch (JsonException)
        {
            throw InvalidIdToken();
        }

        if (!validation.IsValid)
        {
            throw InvalidIdToken();
        }

        JsonElement payload;

        try
        {
            payload = ParseJwtPayload(idToken);
        }
        catch (FormatException)
        {
            throw InvalidIdToken();
        }
        catch (JsonException)
        {
            throw InvalidIdToken();
        }

        if (!payload.TryGetProperty("iat", out var issuedAt) ||
            issuedAt.ValueKind != JsonValueKind.Number ||
            !issuedAt.TryGetInt64(out var issuedAtSeconds))
        {
            throw InvalidIdToken();
        }

        if (issuedAtSeconds >
            DateTimeOffset.UtcNow.ToUnixTimeSeconds() +
            (long)ClockSkew.TotalSeconds)
        {
            throw InvalidIdToken();
        }

        if (expectedNonce is not null &&
            (!payload.TryGetProperty("nonce", out var nonce) ||
             nonce.ValueKind != JsonValueKind.String ||
             !string.Equals(
                 nonce.GetString(),
                 expectedNonce,
                 StringComparison.Ordinal)))
        {
            throw InvalidIdToken();
        }

        if (payload.TryGetProperty("aud", out var audience) &&
            audience.ValueKind == JsonValueKind.Array &&
            audience.GetArrayLength() > 1 &&
            (!payload.TryGetProperty("azp", out var authorizedParty) ||
             authorizedParty.ValueKind != JsonValueKind.String ||
             !string.Equals(
                 authorizedParty.GetString(),
                 clientId,
                 StringComparison.Ordinal)))
        {
            throw InvalidIdToken();
        }

        return ParseIdentity(payload, "invalid_id_token");
    }

    private static ParsedTokenSet ParseAuthorizationTokenResponse(
        JsonElement root,
        IReadOnlyList<string> requestedScopes)
    {
        RequireObject(root);
        RequireBearer(root);

        var expiresIn = ReadExpiresIn(root);

        return new ParsedTokenSet(
            RequiredString(root, "access_token"),
            RequiredString(root, "id_token"),
            OptionalString(root, "refresh_token"),
            expiresIn,
            DateTimeOffset.UtcNow.Add(expiresIn),
            ReadScopes(root, requestedScopes));
    }

    private static AquariTokenSet ParseRefreshTokenResponse(
        JsonElement root,
        AquariTokenSet current)
    {
        RequireObject(root);
        RequireBearer(root);

        var expiresIn = ReadExpiresIn(root);

        return new AquariTokenSet
        {
            Subject = current.Subject,
            AccessToken =
                RequiredString(root, "access_token"),
            IdToken =
                OptionalString(root, "id_token") ??
                current.IdToken,
            RefreshToken =
                OptionalString(root, "refresh_token") ??
                current.RefreshToken,
            ExpiresIn = expiresIn,
            ExpiresAt = DateTimeOffset.UtcNow.Add(expiresIn),
            Scopes = ReadScopes(root, current.Scopes),
        };
    }

    private static AquariIdentity ParseIdentity(
        JsonElement root,
        string failureCode)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new AquariOAuthException(
                failureCode,
                "Aquari returned an invalid identity response.");
        }

        return new AquariIdentity
        {
            Subject =
                RequiredIdentityString(root, "sub", failureCode),
            Handle =
                OptionalIdentityString(
                    root,
                    "preferred_username",
                    failureCode),
            CanonicalHandle =
                OptionalIdentityString(
                    root,
                    CanonicalHandleClaim,
                    failureCode),
            VerifiedHandle =
                OptionalIdentityString(
                    root,
                    VerifiedHandleClaim,
                    failureCode),
            Name =
                OptionalIdentityString(root, "name", failureCode),
            PictureUri =
                OptionalIdentityUri(root, "picture", failureCode),
            Email =
                OptionalIdentityString(root, "email", failureCode),
            EmailVerified =
                OptionalIdentityBoolean(
                    root,
                    "email_verified",
                    failureCode),
        };
    }

    private static void ValidateTokenSet(
        AquariTokenSet tokens)
    {
        if (!string.Equals(
                tokens.TokenType,
                "Bearer",
                StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(tokens.Subject) ||
            string.IsNullOrWhiteSpace(tokens.AccessToken) ||
            string.IsNullOrWhiteSpace(tokens.IdToken) ||
            tokens.ExpiresIn <= TimeSpan.Zero ||
            tokens.Scopes.Count == 0)
        {
            throw new AquariOAuthException(
                "invalid_token_set",
                "Aquari token set is invalid.");
        }
    }

    private static JsonElement ParseJwtPayload(string jwt)
    {
        var segments = jwt.Split('.');

        if (segments.Length != 3)
        {
            throw new FormatException(
                "JWT must contain three segments.");
        }

        var bytes =
            Base64UrlEncoder.DecodeBytes(segments[1]);

        using var document =
            JsonDocument.Parse(bytes);

        return document.RootElement.Clone();
    }

    private static void RequireObject(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw InvalidTokenResponse();
        }
    }

    private static void RequireBearer(JsonElement root)
    {
        if (!string.Equals(
                RequiredString(root, "token_type"),
                "Bearer",
                StringComparison.Ordinal))
        {
            throw InvalidTokenResponse();
        }
    }

    private static TimeSpan ReadExpiresIn(JsonElement root)
    {
        if (!root.TryGetProperty("expires_in", out var value) ||
            value.ValueKind != JsonValueKind.Number ||
            !value.TryGetInt32(out var seconds) ||
            seconds <= 0)
        {
            throw InvalidTokenResponse();
        }

        return TimeSpan.FromSeconds(seconds);
    }

    private static string[] ReadScopes(
        JsonElement root,
        IReadOnlyList<string> fallback)
    {
        if (!root.TryGetProperty("scope", out var value))
        {
            return fallback.ToArray();
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            throw InvalidTokenResponse();
        }

        var text = value.GetString();

        if (string.IsNullOrEmpty(text))
        {
            throw InvalidTokenResponse();
        }

        var scopes =
            text.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries);

        if (scopes.Length == 0 ||
            scopes.Distinct(StringComparer.Ordinal).Count() != scopes.Length)
        {
            throw InvalidTokenResponse();
        }

        return scopes;
    }

    private static string RequiredString(
        JsonElement root,
        string name)
    {
        return OptionalString(root, name) ??
            throw InvalidTokenResponse();
    }

    private static string? OptionalString(
        JsonElement root,
        string name)
    {
        if (!root.TryGetProperty(name, out var value))
        {
            return null;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            throw InvalidTokenResponse();
        }

        var text = value.GetString();

        if (string.IsNullOrEmpty(text) ||
            !string.Equals(
                text,
                text.Trim(),
                StringComparison.Ordinal))
        {
            throw InvalidTokenResponse();
        }

        return text;
    }

    private static string RequiredIdentityString(
        JsonElement root,
        string name,
        string failureCode)
    {
        return OptionalIdentityString(root, name, failureCode) ??
            throw new AquariOAuthException(
                failureCode,
                $"Aquari identity is missing '{name}'.");
    }

    private static string? OptionalIdentityString(
        JsonElement root,
        string name,
        string failureCode)
    {
        if (!root.TryGetProperty(name, out var value))
        {
            return null;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            throw new AquariOAuthException(
                failureCode,
                $"Aquari identity claim '{name}' is invalid.");
        }

        var text = value.GetString();

        if (string.IsNullOrEmpty(text) ||
            !string.Equals(
                text,
                text.Trim(),
                StringComparison.Ordinal))
        {
            throw new AquariOAuthException(
                failureCode,
                $"Aquari identity claim '{name}' is invalid.");
        }

        return text;
    }

    private static Uri? OptionalIdentityUri(
        JsonElement root,
        string name,
        string failureCode)
    {
        var text =
            OptionalIdentityString(root, name, failureCode);

        if (text is null)
        {
            return null;
        }

        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri))
        {
            throw new AquariOAuthException(
                failureCode,
                $"Aquari identity claim '{name}' is invalid.");
        }

        return uri;
    }

    private static bool? OptionalIdentityBoolean(
        JsonElement root,
        string name,
        string failureCode)
    {
        if (!root.TryGetProperty(name, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => throw new AquariOAuthException(
                failureCode,
                $"Aquari identity claim '{name}' is invalid."),
        };
    }

    private static AquariOAuthException InvalidTokenResponse()
    {
        return new AquariOAuthException(
            "invalid_token_response",
            "Aquari returned an invalid token response.");
    }

    private static AquariOAuthException InvalidIdToken()
    {
        return new AquariOAuthException(
            "invalid_id_token",
            "Aquari returned an ID token that failed validation.");
    }

    private sealed record ParsedTokenSet(
        string AccessToken,
        string IdToken,
        string? RefreshToken,
        TimeSpan ExpiresIn,
        DateTimeOffset ExpiresAt,
        IReadOnlyList<string> Scopes);
}
