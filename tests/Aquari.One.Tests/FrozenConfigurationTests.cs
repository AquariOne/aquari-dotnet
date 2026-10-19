using Aquari.One.Configuration;

namespace Aquari.One.Tests;

public sealed class FrozenConfigurationTests
{
    [Fact]
    public void OpenIdScopeIsRequired()
    {
        var options =
            ValidOptions(["profile"]);

        Assert.Throws<AquariConfigurationException>(
            options.Validate);
    }

    [Fact]
    public void DuplicateScopesAreRejected()
    {
        var options =
            ValidOptions(["openid", "openid"]);

        Assert.Throws<AquariConfigurationException>(
            options.Validate);
    }

    [Fact]
    public void WhitespaceInsideScopeIsRejected()
    {
        var options =
            ValidOptions(
                ["openid", "passport.read extra"]);

        Assert.Throws<AquariConfigurationException>(
            options.Validate);
    }

    private static AquariClientOptions ValidOptions(
        IReadOnlyList<string> scopes)
    {
        return new AquariClientOptions
        {
            Issuer =
                new Uri("https://dev.api.aquari.one/"),
            ClientId = "client",
            RedirectUri =
                new Uri("https://app.example/callback"),
            Scopes = scopes,
        };
    }
}
