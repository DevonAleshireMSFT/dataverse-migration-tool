using DataverseMigrationTool.Application.Contracts.Configuration;
using DataverseMigrationTool.Application.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DataverseMigrationTool.Infrastructure.Configuration;

/// <summary>
/// Registers migration configuration services.
/// </summary>
public static class ConfigurationServiceCollectionExtensions
{
    /// <summary>
    /// Adds the validated migration configuration provider.
    /// Source precedence is defaults, host configuration providers such as appsettings, environment variables,
    /// and finally explicit overrides supplied by tests or composition roots.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The host configuration.</param>
    /// <param name="configureOverrides">Optional final overrides.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddMigrationConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<MigrationConfiguration>? configureOverrides = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton<IMigrationConfigurationProvider>(
            new ConfigurationMigrationConfigurationProvider(configuration, configureOverrides));

        return services;
    }
}
