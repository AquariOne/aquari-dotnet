using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Aquari.One.Server.Configuration;
using Aquari.One.Server.Issuer;
using Aquari.One.Server.Webhooks;

namespace Aquari.One.Server.Tests;

public sealed class ServerRuntimeTests
{
    private static readonly DateTimeOffset FixedNow =
        DateTimeOffset.FromUnixTimeSeconds(
            1788297600);

    private static readonly string[] ExpectedIssuerFamilies =
        [
            "Badges",
            "Credentials",
            "Entitlements",
            "Memberships",
            "Stamps",
        ];

    [Fact]
    public async Task MachineContextAndHealthUseApiKeyBearer()
    {
        using var handler =
            new StubHandler(
                request =>
                {
                    Assert.Equal(
                        "Bearer",
                        request.Headers.Authorization?.Scheme);
                    Assert.Equal(
                        "aqk_test_secret",
                        request.Headers.Authorization?.Parameter);

                    return request.RequestUri?.AbsolutePath switch
                    {
                        "/api/v1/machine/context" =>
                            Json(
                                """
                                {
                                  "actorType": "api_key",
                                  "apiKeyId": "key-1",
                                  "keyPrefix": "aqk_test",
                                  "keyName": "Test",
                                  "organizationId": "org-1",
                                  "applicationId": "app-1",
                                  "environmentId": "env-1",
                                  "environmentName": "Production",
                                  "environment": "production",
                                  "scopes": ["machine.context.read"]
                                }
                                """),
                        "/api/v1/machine/health" =>
                            Json(
                                """
                                {
                                  "ok": true,
                                  "actorType": "api_key",
                                  "apiKeyId": "key-1",
                                  "applicationId": "app-1",
                                  "environment": "production"
                                }
                                """),
                        _ =>
                            throw new InvalidOperationException(),
                    };
                });

        using var client =
            CreateClient(new HttpClient(handler));

        var context =
            await client.Machine.ContextAsync();
        var health =
            await client.Machine.HealthAsync();

        Assert.Equal(
            "app-1",
            context.ApplicationId);
        Assert.True(health.Ok);
    }

    [Fact]
    public async Task BadgeIssueUsesIdempotencyAndCanonicalRoute()
    {
        using var handler =
            new StubHandler(
                request =>
                {
                    Assert.Equal(
                        "/api/v1/machine/issuer/badge-definitions/definition-1/signals",
                        request.RequestUri?.AbsolutePath);

                    Assert.Equal(
                        "order-123",
                        request.Headers.GetValues(
                            "Idempotency-Key")
                            .Single());

                    return Json(
                        """
                        {
                          "signal": {
                            "id": "signal-1",
                            "definitionId": "definition-1",
                            "subjectId": "subject-1",
                            "issuer": {
                              "type": "organization",
                              "id": "org-1",
                              "name": "Aquari"
                            },
                            "name": "Founder",
                            "defaultVisibility": "connected",
                            "status": "active",
                            "issuedAt": "2026-09-27T22:00:00Z"
                          },
                          "idempotentReplay": true
                        }
                        """);
                });

        using var client =
            CreateClient(new HttpClient(handler));

        var result =
            await client.Issuer.Badges.IssueAsync(
                "definition-1",
                new AquariBadgeIssueInput
                {
                    SubjectId = "subject-1",
                },
                new AquariIssueOptions
                {
                    IdempotencyKey = "order-123",
                });

        Assert.True(result.IdempotentReplay);
    }

    [Fact]
    public async Task MembershipUpdateCanClearFormalRole()
    {
        string? body = null;

        using var handler =
            new StubHandler(
                async request =>
                {
                    body =
                        await request.Content!
                            .ReadAsStringAsync();

                    return Json(
                        """
                        {
                          "id": "membership-1",
                          "subjectId": "subject-1",
                          "issuer": {
                            "type": "application",
                            "id": "app-1",
                            "name": "App"
                          },
                          "type": "membership",
                          "role": "member",
                          "status": "active",
                          "effective": true,
                          "issuedAt": "2026-09-27T22:00:00Z"
                        }
                        """);
                });

        using var client =
            CreateClient(new HttpClient(handler));

        _ =
            await client.Issuer.Memberships.UpdateAsync(
                "membership-1",
                new AquariMembershipUpdateInput
                {
                    RoleId = null,
                });

        Assert.NotNull(body);

        using var document =
            JsonDocument.Parse(body);

        Assert.Equal(
            JsonValueKind.Null,
            document.RootElement
                .GetProperty("roleId")
                .ValueKind);
    }

    [Fact]
    public void IssuerSurfaceContainsExactlyFiveFamilies()
    {
        var propertyNames =
            typeof(AquariIssuerClient)
                .GetProperties()
                .Select(
                    static property =>
                        property.Name)
                .OrderBy(
                    static value =>
                        value,
                    StringComparer.Ordinal)
                .ToArray();

        Assert.Equal(
            ExpectedIssuerFamilies,
            propertyNames);

        Assert.Null(
            typeof(AquariIssuerClient)
                .GetProperty("Roles"));
    }

    [Fact]
    public void WebhookConformanceVectorMatches()
    {
        const string secret =
            "whsec_test_example";
        const string timestamp =
            "1788297600";
        const string body =
            "{\"id\":\"example\"}";

        Assert.Equal(
            "355f3f5abdc879a7a32dc165cc312529204d8f5b8d7f19ef847fc54e7483ee56",
            Signature(
                secret,
                timestamp,
                body));
    }

    [Fact]
    public void KnownWebhookDataIsTyped()
    {
        const string secret =
            "whsec_test_example";

        var body =
            """
            {
              "id": "event-1",
              "type": "passport.visibility.updated",
              "version": "2026-09-01",
              "created_at": "2026-09-27T22:00:00Z",
              "organization_id": "org-1",
              "application_id": "app-1",
              "environment_id": "env-1",
              "data": {
                "object": {
                  "userId": "user-1",
                  "visibility": "public",
                  "updatedAt": "2026-09-27T22:00:00Z"
                }
              }
            }
            """;

        var timestamp =
            FixedNow.ToUnixTimeSeconds()
                .ToString(
                    System.Globalization.CultureInfo.InvariantCulture);

        var result =
            Verify(
                secret,
                timestamp,
                body);

        Assert.True(result.Known);

        var typed =
            Assert.IsType<AquariPassportVisibilityUpdatedData>(
                result.TypedData);

        Assert.Equal(
            "user-1",
            typed.UserId);
    }

    [Fact]
    public void UnknownValidWebhookEventIsPreserved()
    {
        const string secret =
            "whsec_test_example";

        var body =
            """
            {
              "id": "event-1",
              "type": "future.event.created",
              "version": "2099-01-01",
              "created_at": "2026-09-27T22:00:00Z",
              "organization_id": "org-1",
              "application_id": "app-1",
              "environment_id": "env-1",
              "data": {
                "object": {
                  "future": true
                }
              }
            }
            """;

        var timestamp =
            FixedNow.ToUnixTimeSeconds()
                .ToString(
                    System.Globalization.CultureInfo.InvariantCulture);

        var result =
            Verify(
                secret,
                timestamp,
                body);

        Assert.False(result.Known);
        Assert.Null(result.TypedData);
        Assert.Equal(
            "future.event.created",
            result.Type);
    }

    [Fact]
    public void WebhookBodyTamperingIsRejected()
    {
        const string secret =
            "whsec_test_example";
        const string timestamp =
            "1788297600";

        var body =
            """
            {
              "id": "event-1",
              "type": "future.event.created",
              "version": "2099-01-01",
              "created_at": "2026-09-27T22:00:00Z",
              "organization_id": "org-1",
              "application_id": "app-1",
              "environment_id": "env-1",
              "data": { "object": {} }
            }
            """;

        var signature =
            Signature(
                secret,
                timestamp,
                body);

        var error =
            Assert.Throws<AquariWebhookVerificationException>(
                () =>
                    AquariWebhookVerifier.Verify(
                        new AquariWebhookVerificationOptions
                        {
                            Secret = secret,
                            Headers =
                                new Dictionary<string, string>
                                {
                                    ["Aquari-Timestamp"] =
                                        timestamp,
                                    ["Aquari-Signature"] =
                                        $"v1={signature}",
                                },
                            RawBody =
                                Encoding.UTF8.GetBytes(
                                    body + " "),
                            Now = FixedNow,
                        }));

        Assert.Equal(
            AquariWebhookVerificationFailure.SignatureMismatch,
            error.Reason);
    }

    private static AquariWebhookEvent Verify(
        string secret,
        string timestamp,
        string body)
    {
        return AquariWebhookVerifier.Verify(
            new AquariWebhookVerificationOptions
            {
                Secret = secret,
                Headers =
                    new Dictionary<string, string>
                    {
                        ["Aquari-Timestamp"] =
                            timestamp,
                        ["Aquari-Signature"] =
                            $"v1={Signature(secret, timestamp, body)}",
                    },
                RawBody =
                    Encoding.UTF8.GetBytes(body),
                Now = FixedNow,
            });
    }

    private static AquariServerClient CreateClient(
        HttpClient httpClient)
    {
        return new AquariServerClient(
            new AquariServerClientOptions
            {
                ApiUrl =
                    new Uri(
                        "https://dev.api.aquari.one/"),
                ApiKey = "aqk_test_secret",
                MaxRetries = 0,
            },
            httpClient);
    }

    private static string Signature(
        string secret,
        string timestamp,
        string body)
    {
        using var hmac =
            new HMACSHA256(
                Encoding.UTF8.GetBytes(secret));

        return Convert.ToHexString(
                hmac.ComputeHash(
                    Encoding.UTF8.GetBytes(
                        $"{timestamp}.{body}")))
            .ToLowerInvariant();
    }

    private static HttpResponseMessage Json(
        string body)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content =
                new StringContent(
                    body,
                    Encoding.UTF8,
                    "application/json"),
        };
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>>
            _handler;

        internal StubHandler(
            Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler =
                request =>
                    Task.FromResult(
                        handler(request));
        }

        internal StubHandler(
            Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return _handler(request);
        }
    }
}
