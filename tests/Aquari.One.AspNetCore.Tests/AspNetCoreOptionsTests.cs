using Aquari.One.AspNetCore.Concurrency;
using Aquari.One.AspNetCore.Session;

namespace Aquari.One.AspNetCore.Tests;

public sealed class AspNetCoreOptionsTests
{
    [Fact]
    public void DefaultsMatchFrozenFrameworkContract()
    {
        var options =
            ValidOptions();

        options.Validate();

        Assert.Equal(
            "aquari_session",
            options.SessionCookieName);
        Assert.Equal(
            "aquari_auth",
            options.TransactionCookieName);
        Assert.Equal(
            TimeSpan.FromDays(30),
            options.SessionTtl);
        Assert.Null(
            options.SessionCookieDomain);
        Assert.Null(
            options.SecureCookies);
        Assert.Null(
            options.RefreshLock);
    }

    [Fact]
    public void CookieNamesMustDiffer()
    {
        var options =
            new AquariAspNetCoreOptions
            {
                Issuer =
                    new Uri(
                        "https://dev.api.aquari.one/"),
                ClientId = "client",
                RedirectUri =
                    new Uri(
                        "https://app.example/callback"),
                Scopes = ["openid"],
                SessionStore =
                    new InMemoryAquariSessionStore(),
                SessionCookieName = "same",
                TransactionCookieName = "same",
            };

        Assert.Throws<AquariConfigurationException>(
            options.Validate);
    }

    [Fact]
    public void ProductionCanSupplySharedRefreshLock()
    {
        var refreshLock =
            new InMemoryAquariRefreshLock();

        var options =
            ValidOptions(
                refreshLock);

        options.Validate();

        Assert.Same(
            refreshLock,
            options.RefreshLock);
    }

    private static AquariAspNetCoreOptions ValidOptions(
        IAquariRefreshLock? refreshLock = null)
    {
        return new AquariAspNetCoreOptions
        {
            Issuer =
                new Uri(
                    "https://dev.api.aquari.one/"),
            ClientId = "client",
            RedirectUri =
                new Uri(
                    "https://app.example/callback"),
            Scopes = ["openid"],
            SessionStore =
                new InMemoryAquariSessionStore(),
            RefreshLock = refreshLock,
        };
    }
}
