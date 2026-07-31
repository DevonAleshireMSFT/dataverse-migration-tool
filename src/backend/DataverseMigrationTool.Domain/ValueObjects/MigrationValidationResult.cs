namespace DataverseMigrationTool.Domain.ValueObjects;

public sealed record MigrationValidationResult(
    bool IsValid,
    IReadOnlyCollection<string> Errors,
    IReadOnlyCollection<string> Warnings)
{
    public static MigrationValidationResult Success { get; } = new(
        IsValid: true,
        Errors: Array.Empty<string>(),
        Warnings: Array.Empty<string>());
}

