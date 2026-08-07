using DataverseMigrationTool.Domain.ValueObjects.Validation;

namespace DataverseMigrationTool.Domain.ValueObjects.Compare;

public sealed record EnvironmentComparisonReport
{
    public EnvironmentComparisonReport(
        EnvironmentProfile sourceEnvironment,
        EnvironmentProfile targetEnvironment,
        DateTimeOffset comparedAt,
        IReadOnlyCollection<EnvironmentComparisonFinding> findings,
        MigrationScopeReadiness migrationScope)
    {
        ArgumentNullException.ThrowIfNull(sourceEnvironment);
        ArgumentNullException.ThrowIfNull(targetEnvironment);
        ArgumentNullException.ThrowIfNull(findings);
        ArgumentNullException.ThrowIfNull(migrationScope);

        SourceEnvironment = sourceEnvironment;
        TargetEnvironment = targetEnvironment;
        ComparedAt = comparedAt;
        Findings = findings.ToArray();
        MigrationScope = migrationScope;
    }

    public EnvironmentProfile SourceEnvironment { get; }

    public EnvironmentProfile TargetEnvironment { get; }

    public DateTimeOffset ComparedAt { get; }

    public IReadOnlyCollection<EnvironmentComparisonFinding> Findings { get; }

    public MigrationScopeReadiness MigrationScope { get; }

    public bool IsReady => Counts.Blockers == 0;

    public bool IsBlocked => !IsReady;

    public ValidationSeverityCounts Counts => new(
        Blockers: Findings.Count(finding => finding.Severity == ValidationSeverity.Blocker),
        Warnings: Findings.Count(finding => finding.Severity == ValidationSeverity.Warning),
        Infos: Findings.Count(finding => finding.Severity == ValidationSeverity.Info));

    public IReadOnlyCollection<EnvironmentComparisonFinding> Blockers =>
        Findings.Where(finding => finding.Severity == ValidationSeverity.Blocker).ToArray();

    public IReadOnlyCollection<EnvironmentComparisonFinding> Warnings =>
        Findings.Where(finding => finding.Severity == ValidationSeverity.Warning).ToArray();

    public IReadOnlyCollection<EnvironmentComparisonFinding> Information =>
        Findings.Where(finding => finding.Severity == ValidationSeverity.Info).ToArray();
}
