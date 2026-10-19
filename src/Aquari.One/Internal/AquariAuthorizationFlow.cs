using System.Text.RegularExpressions;
using Aquari.One.Auth;

namespace Aquari.One.Internal;

internal static partial class AquariAuthorizationFlow
{
    private static readonly TimeSpan TransactionMaxAge =
        TimeSpan.FromMinutes(15);

    private static readonly TimeSpan TransactionClockSkew =
        TimeSpan.FromMinutes(1);

    internal static void ValidateTransaction(
        AquariAuthorizationTransaction transaction,
        Uri issuer,
        string clientId,
        Uri redirectUri,
        IReadOnlyList<string> scopes,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        if (transaction.Version != 1 ||
            transaction.Issuer != issuer ||
            !string.Equals(
                transaction.ClientId,
                clientId,
                StringComparison.Ordinal) ||
            transaction.RedirectUri != redirectUri ||
            !transaction.Scopes.SequenceEqual(
                scopes,
                StringComparer.Ordinal) ||
            !VerifierPattern().IsMatch(
                transaction.CodeVerifier) ||
            !StatePattern().IsMatch(
                transaction.State) ||
            !StatePattern().IsMatch(
                transaction.Nonce))
        {
            ThrowInvalidTransaction();
        }

        if (transaction.CreatedAt >
                now + TransactionClockSkew ||
            now - transaction.CreatedAt >
                TransactionMaxAge)
        {
            throw new AquariOAuthException(
                "expired_transaction",
                "Aquari authorization transaction is expired or has an invalid timestamp.");
        }
    }

    internal static AquariAuthorizationCallback ParseCallback(
        Uri callbackUri,
        AquariAuthorizationTransaction transaction,
        bool issuerParameterRequired)
    {
        ArgumentNullException.ThrowIfNull(callbackUri);
        ArgumentNullException.ThrowIfNull(transaction);

        if (!callbackUri.IsAbsoluteUri ||
            !string.IsNullOrEmpty(callbackUri.Fragment) ||
            !MatchesRedirectUri(
                callbackUri,
                transaction.RedirectUri))
        {
            throw new AquariOAuthException(
                "invalid_callback",
                "Aquari authorization callback URI does not match the configured redirect URI.");
        }

        var state =
            ReadSingleParameter(
                callbackUri,
                "state",
                required: true);

        if (!string.Equals(
                state,
                transaction.State,
                StringComparison.Ordinal))
        {
            throw new AquariOAuthException(
                "state_mismatch",
                "Aquari authorization callback state does not match the initiating transaction.");
        }

        var callbackIssuer =
            ReadSingleParameter(
                callbackUri,
                "iss",
                required: false);

        if (callbackIssuer is not null &&
            !string.Equals(
                callbackIssuer,
                transaction.Issuer.AbsoluteUri,
                StringComparison.Ordinal))
        {
            throw new AquariOAuthException(
                "issuer_mismatch",
                "Aquari authorization callback issuer does not match the initiating transaction.");
        }

        if (issuerParameterRequired &&
            callbackIssuer is null)
        {
            throw new AquariOAuthException(
                "missing_issuer",
                "Aquari authorization callback is missing the advertised issuer parameter.");
        }

        var oauthError =
            ReadSingleParameter(
                callbackUri,
                "error",
                required: false);

        var code =
            ReadSingleParameter(
                callbackUri,
                "code",
                required: false);

        if (oauthError is not null &&
            code is not null)
        {
            throw new AquariOAuthException(
                "invalid_callback",
                "Aquari authorization callback cannot contain both a code and an OAuth error.");
        }

        if (oauthError is not null)
        {
            var description =
                ReadSingleParameter(
                    callbackUri,
                    "error_description",
                    required: false);

            throw new AquariOAuthException(
                oauthError,
                description ??
                    $"Aquari returned OAuth error '{oauthError}'.",
                description);
        }

        if (code is null)
        {
            throw new AquariOAuthException(
                "invalid_callback",
                "Aquari authorization callback does not contain an authorization code.");
        }

        return new AquariAuthorizationCallback
        {
            Code = code,
            State = state!,
            Issuer = callbackIssuer,
        };
    }

    internal static Uri BuildAuthorizationUri(
        Uri endpoint,
        AquariAuthorizationTransaction transaction,
        string codeChallenge,
        AquariAuthorizationPrompt? prompt)
    {
        var pairs =
            ParseQuery(endpoint.Query);

        pairs.AddRange(
            new[]
            {
                new KeyValuePair<string, string>(
                    "client_id",
                    transaction.ClientId),
                new KeyValuePair<string, string>(
                    "redirect_uri",
                    transaction.RedirectUri.AbsoluteUri),
                new KeyValuePair<string, string>(
                    "response_type",
                    "code"),
                new KeyValuePair<string, string>(
                    "scope",
                    string.Join(' ', transaction.Scopes)),
                new KeyValuePair<string, string>(
                    "state",
                    transaction.State),
                new KeyValuePair<string, string>(
                    "nonce",
                    transaction.Nonce),
                new KeyValuePair<string, string>(
                    "code_challenge",
                    codeChallenge),
                new KeyValuePair<string, string>(
                    "code_challenge_method",
                    "S256"),
            });

        if (prompt is not null)
        {
            pairs.Add(
                new KeyValuePair<string, string>(
                    "prompt",
                    PromptValue(prompt.Value)));
        }

        var builder =
            new UriBuilder(endpoint)
            {
                Query =
                    string.Join(
                        "&",
                        pairs.Select(
                            static pair =>
                                $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}")),
            };

        return builder.Uri;
    }

    private static bool MatchesRedirectUri(
        Uri callback,
        Uri redirect)
    {
        if (!string.Equals(
                callback.Scheme,
                redirect.Scheme,
                StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(
                callback.Host,
                redirect.Host,
                StringComparison.OrdinalIgnoreCase) ||
            callback.Port != redirect.Port ||
            !string.Equals(
                callback.AbsolutePath,
                redirect.AbsolutePath,
                StringComparison.Ordinal))
        {
            return false;
        }

        var callbackQuery =
            ParseQuery(callback.Query);
        var redirectQuery =
            ParseQuery(redirect.Query);

        foreach (var pair in redirectQuery)
        {
            var index =
                callbackQuery.FindIndex(
                    candidate =>
                        string.Equals(
                            candidate.Key,
                            pair.Key,
                            StringComparison.Ordinal) &&
                        string.Equals(
                            candidate.Value,
                            pair.Value,
                            StringComparison.Ordinal));

            if (index < 0)
            {
                return false;
            }

            callbackQuery.RemoveAt(index);
        }

        return true;
    }

    private static string? ReadSingleParameter(
        Uri callback,
        string name,
        bool required)
    {
        var values =
            ParseQuery(callback.Query)
                .Where(
                    pair =>
                        string.Equals(
                            pair.Key,
                            name,
                            StringComparison.Ordinal))
                .Select(static pair => pair.Value)
                .ToArray();

        if (values.Length > 1)
        {
            throw new AquariOAuthException(
                "invalid_callback",
                $"Aquari authorization callback contains multiple '{name}' parameters.");
        }

        if (values.Length == 0 ||
            string.IsNullOrEmpty(values[0]))
        {
            if (required)
            {
                throw new AquariOAuthException(
                    "invalid_callback",
                    $"Aquari authorization callback is missing '{name}'.");
            }

            return null;
        }

        return values[0];
    }

    private static List<KeyValuePair<string, string>> ParseQuery(
        string query)
    {
        var result =
            new List<KeyValuePair<string, string>>();

        var value =
            query.StartsWith('?')
                ? query[1..]
                : query;

        if (value.Length == 0)
        {
            return result;
        }

        foreach (var part in value.Split('&'))
        {
            var index =
                part.IndexOf('=');

            var rawName =
                index < 0
                    ? part
                    : part[..index];
            var rawValue =
                index < 0
                    ? string.Empty
                    : part[(index + 1)..];

            result.Add(
                new KeyValuePair<string, string>(
                    DecodeQuery(rawName),
                    DecodeQuery(rawValue)));
        }

        return result;
    }

    private static string DecodeQuery(string value)
    {
        return Uri.UnescapeDataString(
            value.Replace('+', ' '));
    }

    private static string PromptValue(
        AquariAuthorizationPrompt prompt)
    {
        return prompt switch
        {
            AquariAuthorizationPrompt.Consent => "consent",
            AquariAuthorizationPrompt.Login => "login",
            AquariAuthorizationPrompt.None => "none",
            AquariAuthorizationPrompt.SelectAccount => "select_account",
            _ => throw new AquariConfigurationException(
                "Aquari authorization prompt is invalid."),
        };
    }

    private static void ThrowInvalidTransaction()
    {
        throw new AquariOAuthException(
            "invalid_transaction",
            "Aquari authorization transaction is invalid or does not match this client configuration.");
    }

    [GeneratedRegex(
        "^[A-Za-z0-9_-]{43}$",
        RegexOptions.CultureInvariant)]
    private static partial Regex StatePattern();

    [GeneratedRegex(
        "^[A-Za-z0-9_-]{64}$",
        RegexOptions.CultureInvariant)]
    private static partial Regex VerifierPattern();
}
