namespace DataverseMigrationTool.Domain.ValueObjects.Metadata;

/// <summary>
/// Alternate key metadata, including the participating Dataverse field logical names.
/// </summary>
public sealed record AlternateKeyMetadata(
    string LogicalName,
    string SchemaName,
    string? DisplayName,
    IReadOnlyList<string> FieldLogicalNames,
    bool IsManaged);
