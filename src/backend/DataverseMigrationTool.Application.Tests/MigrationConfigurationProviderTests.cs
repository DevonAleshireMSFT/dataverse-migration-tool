using DataverseMigrationTool.Application.Contracts.Configuration;
using DataverseMigrationTool.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using DataverseCloud = DataverseMigrationTool.Domain.Enums.DataverseCloud;

namespace DataverseMigrationTool.Application.Tests;

public sealed class MigrationConfigurationProviderTests
{
    [Fact]
    public void GetConfiguration_AppliesDefaultsFileEnvironmentAndOverridesInOrder()
    {
        Dictionary<string, string?> fileValues = new()
        {
            ["DataverseMigrationTool:Source:Name"] = "file-source",
            ["DataverseMigrationTool:Source:Url"] = "https://file-source.crm.dynamics.com",
            ["DataverseMigrationTool:Source:TenantId"] = "11111111-1111-1111-1111-111111111111",
            ["DataverseMigrationTool:Source:Cloud"] = "Public",
            ["DataverseMigrationTool:Source:ClientId"] = "11111111-1111-1111-1111-111111111112",
            ["DataverseMigrationTool:Source:ClientSecretReference:Kind"] = "EnvironmentVariable",
            ["DataverseMigrationTool:Source:ClientSecretReference:Name"] = "DMT_SOURCE_CLIENT_SECRET",
            ["DataverseMigrationTool:Target:Name"] = "file-target",
            ["DataverseMigrationTool:Target:Url"] = "https://file-target.crm.dynamics.com",
            ["DataverseMigrationTool:Target:TenantId"] = "22222222-2222-2222-2222-222222222222",
            ["DataverseMigrationTool:Target:Cloud"] = "Public",
            ["DataverseMigrationTool:Target:ClientId"] = "22222222-2222-2222-2222-222222222223",
            ["DataverseMigrationTool:Target:ClientSecretReference:Kind"] = "EnvironmentVariable",
            ["DataverseMigrationTool:Target:ClientSecretReference:Name"] = "DMT_TARGET_CLIENT_SECRET"
        };
        Dictionary<string, string?> environmentValues = new()
        {
            ["DataverseMigrationTool:Source:Name"] = "env-source",
            ["DataverseMigrationTool:Target:Cloud"] = "GccHigh"
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(fileValues)
            .AddInMemoryCollection(environmentValues)
            .Build();
        ConfigurationMigrationConfigurationProvider provider = new(
            configuration,
            options => options.Target.Cloud = DataverseCloud.Dod);

        MigrationConfiguration result = provider.GetConfiguration();

        Assert.Equal("env-source", result.Source.Name);
        Assert.Equal(DataverseCloud.Dod, result.Target.Cloud);
        Assert.Equal("DMT_SOURCE_CLIENT_SECRET", result.Source.ClientSecretReference.Name);
    }

    [Fact]
    public void Validate_ReturnsClearErrorsForMissingRequiredConfiguration()
    {
        MigrationConfiguration configuration = new();

        IReadOnlyList<string> errors = MigrationConfigurationValidator.Validate(configuration);

        Assert.Contains("DataverseMigrationTool:Source:Name is required.", errors);
        Assert.Contains("DataverseMigrationTool:Source:Url must be an absolute HTTPS URI.", errors);
        Assert.Contains("DataverseMigrationTool:Target:ClientSecretReference:Name is required and must point to a secret reference, not a plaintext secret.", errors);
    }
}
