using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Aquari.One.Internal;

internal static partial class AquariPkce
{
    [GeneratedRegex(
        "^[A-Za-z0-9._~-]{43,128}$",
        RegexOptions.CultureInvariant)]
    private static partial Regex VerifierPattern();

    internal static string CreateSecureRandomValue(int byteLength)
    {
        if (byteLength <= 0)
        {
            throw new AquariConfigurationException(
                "Secure random byte length must be positive.");
        }

        return Base64Url(
            RandomNumberGenerator.GetBytes(byteLength));
    }

    internal static string CreateCodeVerifier()
    {
        return CreateSecureRandomValue(48);
    }

    internal static string CreateS256CodeChallenge(string codeVerifier)
    {
        ArgumentException.ThrowIfNullOrEmpty(codeVerifier);

        if (!VerifierPattern().IsMatch(codeVerifier))
        {
            throw new AquariConfigurationException(
                "PKCE code verifier must contain 43 to 128 RFC 7636 unreserved characters.");
        }

        var digest =
            SHA256.HashData(
                Encoding.ASCII.GetBytes(codeVerifier));

        return Base64Url(digest);
    }

    private static string Base64Url(byte[] value)
    {
        return Convert.ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
