namespace DataverseMigrationTool.Application.Contracts.Configuration;

/// <summary>
/// Root configuration contract for migration environment selection.
/// </summary>
public sealed class MigrationConfiguration
{
    /// <summary>
    /// Gets the configuration section name used by appsettings and environment variables.
    /// </summary>
    public const string SectionName = "DataverseMigrationTool";

    /// <summary>
    /// Gets or sets the source Dataverse environment profile.
    /// </summary>
    public DataverseEnvironmentConfiguration Source { get; set; } = new();

    /// <summary>
    /// Gets or sets the target Dataverse environment profile.
    /// </summary>
    public DataverseEnvironmentConfiguration Target { get; set; } = new();
}
