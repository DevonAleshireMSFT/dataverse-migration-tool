namespace DataverseMigrationTool.Domain.ValueObjects.Metadata;

/// <summary>
/// Field/attribute metadata normalized for table comparison, validation, and UI rendering.
/// </summary>
public sealed record FieldMetadata(
    string LogicalName,
    string SchemaName,
    string DisplayName,
    string? Description,
    MetadataFieldType Type,
    MetadataRequiredLevel RequiredLevel,
    bool IsPrimaryId,
    bool IsPrimaryName,
    bool IsValidForRead,
    bool IsValidForCreate,
    bool IsValidForUpdate,
    IReadOnlyCollection<string> TargetTableLogicalNames,
    string? ChoiceName = null);
