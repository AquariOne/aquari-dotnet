using System.Net;
using System.Text;
using System.Text.Json;

namespace Aquari.One.Tests;

internal static class TestDiscoveryDocument
{
    internal static HttpResponseMessage Success(
        object? overrides = null)
    {
        var values = new Dictionary<string, object?>
        {
            ["issuer"] = "https://dev.api.aquari.one/",
            ["authorization_endpoint"] =
                "https://dev.api.aquari.one/connect/authorize",
            ["token_endpoint"] =
                "https://dev.api.aquari.one/connect/token",
            ["userinfo_endpoint"] =
                "https://dev.api.aquari.one/connect/userinfo",
            ["jwks_uri"] =
                "https://dev.api.aquari.one/.well-known/jwks",
            ["grant_types_supported"] =
                new[] { "authorization_code", "refresh_token" },
            ["response_types_supported"] =
                new[] { "code" },
            ["code_challenge_methods_supported"] =
                new[] { "S256" },
            ["id_token_signing_alg_values_supported"] =
                new[] { "RS256" },
            ["token_endpoint_auth_methods_supported"] =
                new[] { "none", "client_secret_post" },
            ["authorization_response_iss_parameter_supported"] = true,
        };

        if (overrides is not null)
        {
            using var document =
                JsonDocument.Parse(
                    JsonSerializer.Serialize(overrides));

            foreach (var property in document.RootElement.EnumerateObject())
            {
                values[property.Name] =
                    JsonSerializer.Deserialize<object>(
                        property.Value.GetRawText());
            }
        }

        return Json(
            HttpStatusCode.OK,
            values);
    }

    internal static HttpResponseMessage Json(
        HttpStatusCode statusCode,
        object value)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(value),
                Encoding.UTF8,
                "application/json"),
        };
    }
}
