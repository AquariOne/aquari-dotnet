using Aquari.One.Configuration;

namespace Aquari.One.Tests;

public sealed class ConfigurationTests
{
    [Fact]
    public void ValidPublicClientConfigurationIsAccepted()
    {
        var options = new AquariClientOptions
        {
            Issuer = new Uri("https://dev.api.aquari.one/"),
            ClientId = "public-client",
            RedirectUri = new Uri("http://localhost:4200/auth/callback"),
            Scopes = ["openid", "profile", "offline_access", "passport.read"],
        };

        options.Validate();
    }

    [Fact]
    public void EmptyClientSecretIsRejectedWhenSupplied()
    {
        var options = new AquariClientOptions
        {
            Issuer = new Uri("https://dev.api.aquari.one/"),
            ClientId = "client",
            ClientSecret = " ",
            RedirectUri = new Uri("https://app.example/auth/callback"),
            Scopes = ["openid"],
        };

        Assert.Throws<AquariConfigurationException>(options.Validate);
    }

    [Fact]
    public void EmptyScopesAreRejected()
    {
        var options = new AquariClientOptions
        {
            Issuer = new Uri("https://dev.api.aquari.one/"),
            ClientId = "client",
            RedirectUri = new Uri("https://app.example/auth/callback"),
            Scopes = [],
        };

        Assert.Throws<AquariConfigurationException>(options.Validate);
    }
}
