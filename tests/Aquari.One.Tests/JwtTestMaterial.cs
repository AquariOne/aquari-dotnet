using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Aquari.One.Tests;

internal sealed class JwtTestMaterial : IDisposable
{
    private readonly RSA _rsa =
        RSA.Create(2048);

    internal JwtTestMaterial()
    {
        Key =
            new RsaSecurityKey(_rsa)
            {
                KeyId = "test-key",
            };
    }

    internal RsaSecurityKey Key { get; }

    internal string JwksJson
    {
        get
        {
            var parameters =
                _rsa.ExportParameters(
                    includePrivateParameters: false);

            return JsonSerializer.Serialize(
                new
                {
                    keys =
                        new[]
                        {
                            new
                            {
                                kty = "RSA",
                                kid = "test-key",
                                use = "sig",
                                alg = "RS256",
                                n =
                                    Base64UrlEncoder.Encode(
                                        parameters.Modulus ??
                                        throw new InvalidOperationException()),
                                e =
                                    Base64UrlEncoder.Encode(
                                        parameters.Exponent ??
                                        throw new InvalidOperationException()),
                            },
                        },
                });
        }
    }

    internal string CreateIdToken(
        string issuer,
        string audience,
        string subject,
        string nonce)
    {
        var descriptor =
            new SecurityTokenDescriptor
            {
                Issuer = issuer,
                Audience = audience,
                Claims =
                    new Dictionary<string, object>
                    {
                        ["sub"] = subject,
                        ["nonce"] = nonce,
                        ["preferred_username"] =
                            "@tester.aquari.one",
                        ["name"] = "Tester",
                        ["email"] =
                            "tester@example.com",
                        ["email_verified"] = true,
                    },
                IssuedAt = DateTime.UtcNow,
                Expires =
                    DateTime.UtcNow.AddMinutes(5),
                SigningCredentials =
                    new SigningCredentials(
                        Key,
                        SecurityAlgorithms.RsaSha256),
            };

        return new JsonWebTokenHandler()
            .CreateToken(descriptor);
    }

    public void Dispose()
    {
        _rsa.Dispose();
    }
}
