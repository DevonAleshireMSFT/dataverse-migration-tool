namespace DataverseMigrationTool.Domain.ValueObjects.Metadata;

/// <summary>
/// A global or local Dataverse choice set detached from SDK-specific option-set metadata.
/// </summary>
public sealed record ChoiceMetadata(
    string Name,
    string? DisplayName,
    ChoiceKind Kind,
    IReadOnlyList<ChoiceOption> Options,
    string? TableLogicalName = null,
    string? FieldLogicalName = null,
    bool IsManaged = false);
