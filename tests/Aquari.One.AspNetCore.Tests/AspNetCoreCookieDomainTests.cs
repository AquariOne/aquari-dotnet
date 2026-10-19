using Aquari.One.AspNetCore.Concurrency;
using Aquari.One.AspNetCore.Session;
using Aquari.One.Configuration;

namespace Aquari.One.AspNetCore.Tests;

public sealed class AspNetCoreCookieDomainTests
{
    [Theory]
    [InlineData("gemira.app")]
    [InlineData(".gemira.app")]
    public void SessionCookieDomainAllowsRedirectParent(
        string domain)
    {
        var options =
            CreateOptions(
                new Uri(
                    "https://dev.gemira.app/auth/callback"),
                domain);

        options.Validate();

        Assert.Equal(
            domain,
            options.SessionCookieDomain);
    }

    [Fact]
    public void SessionCookieDomainRejectsUnrelatedDomain()
    {
        var options =
            CreateOptions(
                new Uri(
                    "https://dev.gemira.app/auth/callback"),
                "evil.example");

        var exception =
            Assert.Throws<AquariConfigurationException>(
                options.Validate);

        Assert.Contains(
            "redirect host",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SignOutDeletesSessionCookieWithConfiguredDomain()
    {
        var redirectUri =
            new Uri(
                "https://dev.gemira.app/auth/callback");

        var store =
            new InMemoryAquariSessionStore();

        using var handler =
            new AspNetCoreTestHttp(
                (_, _) =>
                    Task.FromResult(
                        AspNetCoreTestHttp.Json(
                            "{}")));

        using var httpClient =
            new HttpClient(handler);

        using var client =
            new AquariClient(
                new AquariClientOptions
                {
                    Issuer =
                        new Uri(
                            "https://api.aquari.one/"),
                    ClientId =
                        "gemira-web",
                    RedirectUri =
                        redirectUri,
                    Scopes =
                        [
                            "openid",
                            "profile",
                            "offline_access",
                        ],
                    MaxRetries =
                        0,
                },
                httpClient);

        var auth =
            new AquariAspNetCoreAuth(
                new AquariAspNetCoreOptions
                {
                    Issuer =
                        new Uri(
                            "https://api.aquari.one/"),
                    ClientId =
                        "gemira-web",
                    RedirectUri =
                        redirectUri,
                    Scopes =
                        [
                            "openid",
                            "profile",
                            "offline_access",
                        ],
                    SessionStore =
                        store,
                    RefreshLock =
                        new InMemoryAquariRefreshLock(),
                    SessionCookieName =
                        "gemira_dev_aquari_session",
                    SessionCookieDomain =
                        ".gemira.app",
                    SecureCookies =
                        true,
                    MaxRetries =
                        0,
                },
                client);

        var context =
            AspNetCoreTestState.ContextWithSession(
                "not-present");

        await auth.SignOutAsync(
            context);

        var setCookie =
            context.Response.Headers
                .SetCookie
                .ToString();

        Assert.Contains(
            "gemira_dev_aquari_session=",
            setCookie,
            StringComparison.Ordinal);
        Assert.Contains(
            "domain=gemira.app",
            setCookie,
            StringComparison.OrdinalIgnoreCase);
    }

    private static AquariAspNetCoreOptions CreateOptions(
        Uri redirectUri,
        string? sessionCookieDomain)
    {
        return new AquariAspNetCoreOptions
        {
            Issuer =
                new Uri(
                    "https://api.aquari.one/"),
            ClientId =
                "gemira-web",
            RedirectUri =
                redirectUri,
            Scopes =
                [
                    "openid",
                    "profile",
                    "offline_access",
                ],
            SessionStore =
                new InMemoryAquariSessionStore(),
            SessionCookieDomain =
                sessionCookieDomain,
        };
    }
}
