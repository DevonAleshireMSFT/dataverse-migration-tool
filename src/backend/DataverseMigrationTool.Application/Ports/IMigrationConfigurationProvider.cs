using DataverseMigrationTool.Application.Contracts.Configuration;
using DataverseMigrationTool.Domain.ValueObjects;

namespace DataverseMigrationTool.Application.Ports;

/// <summary>
/// Provides validated migration configuration to application services without exposing infrastructure concerns.
/// </summary>
public interface IMigrationConfigurationProvider
{
    /// <summary>
    /// Gets the validated migration configuration contract.
    /// </summary>
    /// <returns>The validated migration configuration.</returns>
    MigrationConfiguration GetConfiguration();

    /// <summary>
    /// Gets the configured source Dataverse environment profile.
    /// </summary>
    /// <returns>The source environment profile.</returns>
    EnvironmentProfile GetSourceEnvironment();

    /// <summary>
    /// Gets the configured target Dataverse environment profile.
    /// </summary>
    /// <returns>The target environment profile.</returns>
    EnvironmentProfile GetTargetEnvironment();
}
