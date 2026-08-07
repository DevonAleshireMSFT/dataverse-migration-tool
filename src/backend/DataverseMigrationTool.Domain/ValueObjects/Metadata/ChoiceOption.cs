namespace DataverseMigrationTool.Domain.ValueObjects.Metadata;

/// <summary>
/// A Dataverse choice/option value suitable for compare, validation, and UI display.
/// </summary>
public sealed record ChoiceOption(
    int Value,
    string Label,
    string? Description = null,
    int? DisplayOrder = null);
