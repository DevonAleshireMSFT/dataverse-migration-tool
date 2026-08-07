namespace DataverseMigrationTool.Domain.ValueObjects.Metadata;

/// <summary>
/// Describes supported Dataverse relationship directions for metadata consumers.
/// </summary>
public enum MetadataRelationshipType
{
    OneToMany,
    ManyToOne,
    ManyToMany
}
