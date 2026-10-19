using Aquari.One.Server.Configuration;

namespace Aquari.One.Server.Tests;

public sealed class ServerConfigurationTests
{
    [Fact]
    public void ValidServerConfigurationIsAccepted()
    {
        var options = new AquariServerClientOptions
        {
            ApiUrl = new Uri("https://dev.api.aquari.one/"),
            ApiKey = "aqk_test_not_a_real_key",
        };

        options.Validate();
    }

    [Fact]
    public void EmptyApiKeyIsRejected()
    {
        var options = new AquariServerClientOptions
        {
            ApiUrl = new Uri("https://dev.api.aquari.one/"),
            ApiKey = " ",
        };

        Assert.Throws<AquariConfigurationException>(options.Validate);
    }
}
