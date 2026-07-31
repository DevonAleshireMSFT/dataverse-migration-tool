namespace DataverseMigrationTool.Domain.ValueObjects;

public sealed record ComponentSelection(
    bool IncludeData,
    bool IncludeSolutions,
    IReadOnlyCollection<string> TableLogicalNames,
    IReadOnlyCollection<string> SolutionUniqueNames)
{
    public static ComponentSelection Empty { get; } = new(
        IncludeData: false,
        IncludeSolutions: false,
        TableLogicalNames: Array.Empty<string>(),
        SolutionUniqueNames: Array.Empty<string>());
}

