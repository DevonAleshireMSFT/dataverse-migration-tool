using DataverseMigrationTool.Domain.Enums;
using DataverseMigrationTool.Domain.ValueObjects;

namespace DataverseMigrationTool.Application.Contracts.Configuration;

/// <summary>
/// Defines the configurable, non-secret settings for one Dataverse environment profile.
/// </summary>
public sealed class DataverseEnvironmentConfiguration
{
    /// <summary>
    /// Gets or sets the operator-friendly profile name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the absolute HTTPS Dataverse environment URL.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Microsoft Entra tenant identifier for this environment.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the Dataverse cloud for endpoint and authority resolution.
    /// </summary>
    public DataverseCloud Cloud { get; set; } = DataverseCloud.Public;

    /// <summary>
    /// Gets or sets the application/client identifier used for this environment.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the reference used to locate the client secret at runtime.
    /// The referenced secret value is intentionally not part of this contract.
    /// </summary>
    public SecretReference ClientSecretReference { get; set; } = new();

    /// <summary>
    /// Converts this configuration contract to the domain environment profile.
    /// </summary>
    /// <returns>The domain environment profile.</returns>
    public EnvironmentProfile ToEnvironmentProfile() =>
        new(Name, new Uri(Url, UriKind.Absolute), TenantId, Cloud);
}
