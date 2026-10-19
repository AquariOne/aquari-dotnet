using Aquari.One.Configuration;

namespace Aquari.One.Tests;

public sealed class UriPolicyTests
{
    [Fact]
    public void RemoteHttpIssuerIsRejected()
    {
        var options =
            ValidOptions(
                new Uri("http://example.com/"));

        Assert.Throws<AquariConfigurationException>(
            options.Validate);
    }

    [Fact]
    public void LocalHttpIssuerIsAccepted()
    {
        var options =
            ValidOptions(
                new Uri("http://localhost:5000/"));

        options.Validate();
    }

    [Fact]
    public void IssuerQueryStringIsRejected()
    {
        var options =
            ValidOptions(
                new Uri(
                    "https://dev.api.aquari.one/?oops=1"));

        Assert.Throws<AquariConfigurationException>(
            options.Validate);
    }

    private static AquariClientOptions ValidOptions(Uri issuer)
    {
        return new AquariClientOptions
        {
            Issuer = issuer,
            ClientId = "client",
            RedirectUri =
                new Uri("https://app.example/auth/callback"),
            Scopes = ["openid"],
        };
    }
}
