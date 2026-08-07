namespace DataverseMigrationTool.Domain.ValueObjects.Validation;

public sealed record ValidationSeverityCounts(int Blockers, int Warnings, int Infos)
{
    public int Total => Blockers + Warnings + Infos;

    public int For(ValidationSeverity severity) => severity switch
    {
        ValidationSeverity.Blocker => Blockers,
        ValidationSeverity.Warning => Warnings,
        ValidationSeverity.Info => Infos,
        _ => 0
    };
}
