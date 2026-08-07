namespace DataverseMigrationTool.Domain.ValueObjects.Validation;

public sealed record ValidationReport
{
    public ValidationReport(IReadOnlyCollection<ValidationFinding> findings)
    {
        ArgumentNullException.ThrowIfNull(findings);

        Findings = findings.ToArray();
    }

    public static ValidationReport Empty { get; } = new(Array.Empty<ValidationFinding>());

    public IReadOnlyCollection<ValidationFinding> Findings { get; }

    public bool Passed => Counts.Blockers == 0;

    public bool Failed => !Passed;

    public ValidationSeverityCounts Counts => new(
        Blockers: Findings.Count(finding => finding.Severity == ValidationSeverity.Blocker),
        Warnings: Findings.Count(finding => finding.Severity == ValidationSeverity.Warning),
        Infos: Findings.Count(finding => finding.Severity == ValidationSeverity.Info));

    public IReadOnlyCollection<ValidationFinding> Blockers =>
        Findings.Where(finding => finding.Severity == ValidationSeverity.Blocker).ToArray();

    public IReadOnlyCollection<ValidationFinding> Warnings =>
        Findings.Where(finding => finding.Severity == ValidationSeverity.Warning).ToArray();

    public IReadOnlyCollection<ValidationFinding> Information =>
        Findings.Where(finding => finding.Severity == ValidationSeverity.Info).ToArray();

    public static ValidationReport FromFindings(IEnumerable<ValidationFinding> findings)
    {
        ArgumentNullException.ThrowIfNull(findings);

        return new ValidationReport(findings.ToArray());
    }
}
