using Intellishelf.Api.Configuration;
using Intellishelf.Domain.Users.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Intellishelf.Unit.Tests.DependencyInjection;

public class OptionsValidationTests
{
    public static TheoryData<string, bool> RequiredKeyCases()
    {
        var data = new TheoryData<string, bool>();

        foreach (var key in DiTestConfig.RequiredKeys)
        {
            data.Add(key, true);
            data.Add(key, false);
        }

        return data;
    }

    [Fact]
    public void GivenCompleteConfig_WhenValidatingOnStart_ThenPassesAndOptionsCarryBoundValues()
    {
        var config = DiTestConfig.Complete();
        using var provider = DiTestConfig.AddConfigured(DiTestConfig.Build(config)).BuildServiceProvider();

        provider.GetRequiredService<IStartupValidator>().Validate();

        Assert.Equal(config["Database:ConnectionString"], provider.GetRequiredService<IOptions<DatabaseConfig>>().Value.ConnectionString);
        Assert.Equal(config["Database:DatabaseName"], provider.GetRequiredService<IOptions<DatabaseConfig>>().Value.DatabaseName);
        Assert.Equal(config["Azure:StorageConnectionString"], provider.GetRequiredService<IOptions<AzureConfig>>().Value.StorageConnectionString);
        Assert.Equal(config["Azure:StorageContainer"], provider.GetRequiredService<IOptions<AzureConfig>>().Value.StorageContainer);
        Assert.Equal(config["Ai:OpenAiApiKey"], provider.GetRequiredService<IOptions<AiConfig>>().Value.OpenAiApiKey);

        var auth = provider.GetRequiredService<IOptions<AuthConfig>>().Value;
        Assert.Equal(config["Auth:Key"], auth.Key);
        Assert.Equal(config["Auth:Google:ClientId"], auth.Google.ClientId);
        Assert.Equal(config["Auth:Google:ClientSecret"], auth.Google.ClientSecret);
    }

    [Theory]
    [MemberData(nameof(RequiredKeyCases))]
    public void GivenRequiredKeyMissingOrEmpty_WhenValidatingOnStart_ThenThrowsNamingMemberWithoutValues(string key, bool remove)
    {
        var config = DiTestConfig.Complete();
        config["Ai:AzureVisionKey"] = "synthetic-vision-key";

        if (remove)
        {
            config.Remove(key);
        }
        else
        {
            config[key] = "";
        }

        var leakCandidates = config.Where(x => x.Key != key).Select(x => x.Value!).ToArray();
        using var provider = DiTestConfig.AddConfigured(DiTestConfig.Build(config)).BuildServiceProvider();

        var exception = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IStartupValidator>().Validate());

        Assert.Contains(key.Split(':')[^1], exception.Message);
        Assert.All(leakCandidates, value => Assert.DoesNotContain(value, exception.Message));
    }

    [Theory]
    [InlineData("Database")]
    [InlineData("Azure")]
    [InlineData("Ai")]
    [InlineData("Auth")]
    public void GivenSectionAbsent_WhenRegistering_ThenThrowsInvalidOperationException(string section)
    {
        var config = DiTestConfig.Complete()
            .Where(x => !x.Key.StartsWith(section + ":"))
            .ToDictionary(x => x.Key, x => x.Value);

        Assert.Throws<InvalidOperationException>(() => DiTestConfig.AddConfigured(DiTestConfig.Build(config)));
    }

    [Fact]
    public void GivenVisionKeysAbsent_WhenValidatingOnStart_ThenPasses()
    {
        var config = DiTestConfig.Complete();
        Assert.DoesNotContain(config.Keys, key => key.StartsWith("Ai:AzureVision"));
        using var provider = DiTestConfig.AddConfigured(DiTestConfig.Build(config)).BuildServiceProvider();

        provider.GetRequiredService<IStartupValidator>().Validate();

        Assert.Null(provider.GetRequiredService<IOptions<AiConfig>>().Value.AzureVisionEndpoint);
    }
}
