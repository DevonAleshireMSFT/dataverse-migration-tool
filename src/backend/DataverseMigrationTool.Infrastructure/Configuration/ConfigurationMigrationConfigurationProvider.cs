using DataverseMigrationTool.Application.Contracts.Configuration;
using DataverseMigrationTool.Application.Ports;
using DataverseMigrationTool.Domain.Enums;
using DataverseMigrationTool.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;

namespace DataverseMigrationTool.Infrastructure.Configuration;

/// <summary>
/// Reads migration configuration from the host configuration pipeline.
/// </summary>
public sealed class ConfigurationMigrationConfigurationProvider(
    IConfiguration configuration,
    Action<MigrationConfiguration>? configureOverrides = null) : IMigrationConfigurationProvider
{
    public MigrationConfiguration GetConfiguration()
    {
        MigrationConfiguration migrationConfiguration = new();
        IConfigurationSection section = configuration.GetSection(MigrationConfiguration.SectionName);

        ApplyEnvironment(section.GetSection(nameof(MigrationConfiguration.Source)), migrationConfiguration.Source);
        ApplyEnvironment(section.GetSection(nameof(MigrationConfiguration.Target)), migrationConfiguration.Target);
        configureOverrides?.Invoke(migrationConfiguration);

        MigrationConfigurationValidator.ThrowIfInvalid(migrationConfiguration);

        return migrationConfiguration;
    }

    public EnvironmentProfile GetSourceEnvironment() => GetConfiguration().Source.ToEnvironmentProfile();

    public EnvironmentProfile GetTargetEnvironment() => GetConfiguration().Target.ToEnvironmentProfile();

    private static void ApplyEnvironment(IConfigurationSection section, DataverseEnvironmentConfiguration environment)
    {
        environment.Name = ReadString(section, nameof(DataverseEnvironmentConfiguration.Name), environment.Name);
        environment.Url = ReadString(section, nameof(DataverseEnvironmentConfiguration.Url), environment.Url);
        environment.ClientId = ReadString(section, nameof(DataverseEnvironmentConfiguration.ClientId), environment.ClientId);
        environment.TenantId = ReadGuid(section, nameof(DataverseEnvironmentConfiguration.TenantId), environment.TenantId);
        environment.Cloud = ReadEnum(section, nameof(DataverseEnvironmentConfiguration.Cloud), environment.Cloud);

        IConfigurationSection secretSection = section.GetSection(nameof(DataverseEnvironmentConfiguration.ClientSecretReference));
        environment.ClientSecretReference.Kind = ReadEnum(
            secretSection,
            nameof(SecretReference.Kind),
            environment.ClientSecretReference.Kind);
        environment.ClientSecretReference.Name = ReadString(
            secretSection,
            nameof(SecretReference.Name),
            environment.ClientSecretReference.Name);
    }

    private static string ReadString(IConfigurationSection section, string key, string currentValue) =>
        section[key] is { } value ? value : currentValue;

    private static Guid ReadGuid(IConfigurationSection section, string key, Guid currentValue) =>
        Guid.TryParse(section[key], out Guid value) ? value : currentValue;

    private static TEnum ReadEnum<TEnum>(IConfigurationSection section, string key, TEnum currentValue)
        where TEnum : struct, Enum =>
        Enum.TryParse(section[key], ignoreCase: true, out TEnum value) ? value : currentValue;
}
