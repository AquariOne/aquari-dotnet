using System.Text.Json;
using Aquari.One.AspNetCore.Session;
using Aquari.One.Auth;
using Microsoft.AspNetCore.Http;

namespace Aquari.One.AspNetCore.Tests;

internal static class AspNetCoreTestState
{
    private static readonly JsonSerializerOptions Options =
        new(JsonSerializerDefaults.Web);

    internal static async Task SeedSessionAsync(
        IAquariSessionStore store,
        string sessionId,
        AquariTokenSet tokens,
        AquariIdentity identity)
    {
        var raw =
            JsonSerializer.Serialize(
                new
                {
                    version = 1,
                    tokens,
                    identity,
                },
                Options);

        await store.SetAsync(
            $"aquari:session:{sessionId}",
            raw,
            DateTimeOffset.UtcNow.AddHours(1));
    }

    internal static DefaultHttpContext ContextWithSession(
        string sessionId)
    {
        var context =
            new DefaultHttpContext();

        context.Request.Headers.Cookie =
            $"aquari_session={sessionId}";

        context.Response.Body =
            new MemoryStream();

        return context;
    }

    internal static AquariTokenSet NearExpiryTokens(
        string accessToken = "access-old",
        string refreshToken = "refresh-old")
    {
        return new AquariTokenSet
        {
            Subject = "subject-1",
            AccessToken = accessToken,
            IdToken = "id-old",
            RefreshToken = refreshToken,
            ExpiresIn = TimeSpan.FromMinutes(15),
            ExpiresAt =
                DateTimeOffset.UtcNow.AddSeconds(5),
            Scopes =
                ["openid", "offline_access"],
        };
    }

    internal static AquariIdentity Identity()
    {
        return new AquariIdentity
        {
            Subject = "subject-1",
            Handle = "@tester.aquari.one",
            Name = "Tester",
        };
    }
}
