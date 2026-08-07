namespace DataverseMigrationTool.Domain.ValueObjects.Metadata;

/// <summary>
/// Table/entity metadata containing fields, relationships, and alternate keys.
/// </summary>
public sealed record TableMetadata(
    string LogicalName,
    string SchemaName,
    string DisplayName,
    string? Description,
    bool IsCustomTable,
    bool IsActivity,
    bool IsIntersect,
    IReadOnlyList<FieldMetadata> Fields,
    IReadOnlyList<RelationshipMetadata> Relationships,
    IReadOnlyList<AlternateKeyMetadata> AlternateKeys);
