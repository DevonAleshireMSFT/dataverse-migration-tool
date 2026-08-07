namespace DataverseMigrationTool.Domain.ValueObjects.Metadata;

/// <summary>
/// Relationship metadata for one-to-many, many-to-one, and many-to-many Dataverse relationships.
/// </summary>
public sealed record RelationshipMetadata(
    string SchemaName,
    MetadataRelationshipType Type,
    string ReferencingTableLogicalName,
    string? ReferencingFieldLogicalName,
    string ReferencedTableLogicalName,
    string? ReferencedFieldLogicalName,
    string? IntersectTableName,
    bool IsCustomRelationship);
