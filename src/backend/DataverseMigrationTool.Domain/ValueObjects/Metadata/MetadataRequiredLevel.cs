namespace DataverseMigrationTool.Domain.ValueObjects.Metadata;

/// <summary>
/// Describes Dataverse field requirement levels without leaking SDK types.
/// </summary>
public enum MetadataRequiredLevel
{
    None,
    SystemRequired,
    ApplicationRequired,
    Recommended
}
