using System.Net;
using System.Text;
using Aquari.One.Configuration;
using Aquari.One.Passport;

namespace Aquari.One.Tests;

public sealed class PassportTrustTests
{
    [Fact]
    public async Task ConnectedPassportAndTrustAreParsed()
    {
        using var handler =
            new StubHttpMessageHandler(
                (request, _) =>
                {
                    var path =
                        request.RequestUri?.PathAndQuery;

                    if (path == "/api/v1/passport")
                    {
                        return Task.FromResult(
                            Json(
                                """
                                {
                                  "passport": {
                                    "id": "passport-1",
                                    "passportNumber": "AQ-00000001",
                                    "subjectId": "subject-1",
                                    "profile": {
                                      "displayName": "Tester",
                                      "handle": "@tester.aquari.one",
                                      "publicUrl": "https://aquari.one/@tester",
                                      "verified": true,
                                      "visibility": "connected"
                                    },
                                    "signals": { "total": 0 }
                                  }
                                }
                                """));
                    }

                    if (path ==
                        "/api/v1/passport/trust/evaluation")
                    {
                        return Task.FromResult(
                            Json(
                                """
                                {
                                  "evaluationId": "evaluation-1",
                                  "subjectId": "subject-1",
                                  "consumerApplicationId": "app-1",
                                  "evaluatedAt": "2026-09-27T22:00:00Z",
                                  "outcomes": []
                                }
                                """));
                    }

                    throw new InvalidOperationException(
                        $"Unexpected path: {path}");
                });

        using var client =
            CreateClient(new HttpClient(handler));

        var passport =
            await client.Passport.GetAsync(
                "access-token");

        var trust =
            await client.Trust.EvaluateAsync(
                "access-token");

        Assert.Equal(
            "AQ-00000001",
            passport.PassportNumber);
        Assert.Equal(
            "evaluation-1",
            trust.EvaluationId);
    }

    [Fact]
    public async Task UnknownSignalFamilyIsPreservedAsUnknown()
    {
        using var handler =
            new StubHttpMessageHandler(
                (_, _) =>
                    Task.FromResult(
                        Json(
                            """
                            {
                              "items": [
                                {
                                  "id": "signal-1",
                                  "subjectId": "subject-1",
                                  "sourceType": "future_source",
                                  "type": "future_type",
                                  "name": "Future",
                                  "description": "",
                                  "issuer": {
                                    "type": "organization",
                                    "id": "org-1",
                                    "name": "Aquari"
                                  },
                                  "issuedAt": "2026-09-27T22:00:00Z",
                                  "visibility": "connected",
                                  "verified": true
                                }
                              ],
                              "page": {
                                "hasMore": false
                              }
                            }
                            """)));

        using var client =
            CreateClient(new HttpClient(handler));

        var page =
            await client.Passport.Signals.ListAsync(
                "access-token");

        Assert.Equal(
            AquariPassportSignalFamily.Unknown,
            Assert.Single(page.Items).Family);
    }

    private static AquariClient CreateClient(
        HttpClient client)
    {
        return new AquariClient(
            new AquariClientOptions
            {
                Issuer =
                    new Uri(
                        "https://dev.api.aquari.one/"),
                ClientId = "client",
                RedirectUri =
                    new Uri(
                        "https://app.example/callback"),
                Scopes =
                    ["openid", "passport.read"],
                MaxRetries = 0,
            },
            client);
    }

    private static HttpResponseMessage Json(
        string value)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content =
                new StringContent(
                    value,
                    Encoding.UTF8,
                    "application/json"),
        };
    }
}
