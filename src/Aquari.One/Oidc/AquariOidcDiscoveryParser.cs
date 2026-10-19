using System.Text.Json;
using Aquari.One.Internal;

namespace Aquari.One.Oidc;

internal static class AquariOidcDiscoveryParser
{
    internal static AquariOidcDiscovery Parse(
        JsonElement root,
        Uri expectedIssuer)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new AquariConfigurationException(
                "Aquari OIDC discovery returned an invalid document.");
        }

        var issuerText = RequiredString(root, "issuer");

        if (!string.Equals(
                issuerText,
                expectedIssuer.AbsoluteUri,
                StringComparison.Ordinal))
        {
            throw new AquariConfigurationException(
                "Aquari OIDC discovery issuer does not exactly match the configured issuer.");
        }

        var grantTypes =
            RequiredStringArray(root, "grant_types_supported");
        var responseTypes =
            RequiredStringArray(root, "response_types_supported");
        var challengeMethods =
            RequiredStringArray(root, "code_challenge_methods_supported");
        var signingAlgorithms =
            RequiredStringArray(root, "id_token_signing_alg_values_supported");

        RequireValue(
            grantTypes,
            "authorization_code",
            "Aquari OIDC discovery does not advertise the authorization-code grant.");
        RequireValue(
            grantTypes,
            "refresh_token",
            "Aquari OIDC discovery does not advertise the refresh-token grant.");
        RequireValue(
            responseTypes,
            "code",
            "Aquari OIDC discovery does not advertise the code response type.");
        RequireValue(
            challengeMethods,
            "S256",
            "Aquari OIDC discovery does not advertise S256 PKCE.");
        RequireValue(
            signingAlgorithms,
            "RS256",
            "Aquari OIDC discovery does not advertise RS256 ID-token signing.");

        return new AquariOidcDiscovery
        {
            Issuer = new Uri(issuerText, UriKind.Absolute),
            AuthorizationEndpoint = AquariUriPolicy.ReadSecureEndpoint(
                RequiredString(root, "authorization_endpoint"),
                "authorization_endpoint"),
            TokenEndpoint = AquariUriPolicy.ReadSecureEndpoint(
                RequiredString(root, "token_endpoint"),
                "token_endpoint"),
            UserInfoEndpoint = AquariUriPolicy.ReadSecureEndpoint(
                RequiredString(root, "userinfo_endpoint"),
                "userinfo_endpoint"),
            JwksUri = AquariUriPolicy.ReadSecureEndpoint(
                RequiredString(root, "jwks_uri"),
                "jwks_uri"),
            GrantTypesSupported = grantTypes,
            ResponseTypesSupported = responseTypes,
            CodeChallengeMethodsSupported = challengeMethods,
            IdTokenSigningAlgorithmsSupported = signingAlgorithms,
            TokenEndpointAuthMethodsSupported =
                OptionalStringArray(
                    root,
                    "token_endpoint_auth_methods_supported"),
            AuthorizationResponseIssuerParameterSupported =
                OptionalBoolean(
                    root,
                    "authorization_response_iss_parameter_supported") ??
                false,
        };
    }

    private static string RequiredString(
        JsonElement root,
        string name)
    {
        if (!root.TryGetProperty(name, out var value) ||
            value.ValueKind != JsonValueKind.String)
        {
            throw InvalidField(
                name,
                "must be a non-empty normalized string");
        }

        var text = value.GetString();

        if (string.IsNullOrEmpty(text) ||
            !string.Equals(
                text,
                text.Trim(),
                StringComparison.Ordinal))
        {
            throw InvalidField(
                name,
                "must be a non-empty normalized string");
        }

        return text;
    }

    private static IReadOnlyList<string> RequiredStringArray(
        JsonElement root,
        string name)
    {
        var values = OptionalStringArray(root, name);

        if (values.Count == 0)
        {
            throw InvalidField(
                name,
                "must be a non-empty string array");
        }

        return values;
    }

    private static IReadOnlyList<string> OptionalStringArray(
        JsonElement root,
        string name)
    {
        if (!root.TryGetProperty(name, out var value))
        {
            return Array.Empty<string>();
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            throw InvalidField(
                name,
                "must be a string array");
        }

        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
            {
                throw InvalidField(
                    name,
                    "must contain only strings");
            }

            var text = item.GetString();

            if (string.IsNullOrEmpty(text) ||
                !string.Equals(
                    text,
                    text.Trim(),
                    StringComparison.Ordinal) ||
                !seen.Add(text))
            {
                throw InvalidField(
                    name,
                    "contains an invalid or duplicate value");
            }

            result.Add(text);
        }

        return result;
    }

    private static bool? OptionalBoolean(
        JsonElement root,
        string name)
    {
        if (!root.TryGetProperty(name, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => throw InvalidField(name, "must be a boolean"),
        };
    }

    private static void RequireValue(
        IReadOnlyList<string> values,
        string required,
        string message)
    {
        if (!values.Contains(required, StringComparer.Ordinal))
        {
            throw new AquariConfigurationException(message);
        }
    }

    private static AquariConfigurationException InvalidField(
        string name,
        string requirement)
    {
        return new AquariConfigurationException(
            $"Aquari OIDC discovery field '{name}' {requirement}.");
    }
}
