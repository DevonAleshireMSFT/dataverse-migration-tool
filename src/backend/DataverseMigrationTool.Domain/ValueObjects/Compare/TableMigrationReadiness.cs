using DataverseMigrationTool.Domain.ValueObjects.Validation;

namespace DataverseMigrationTool.Domain.ValueObjects.Compare;

public sealed record TableMigrationReadiness(
    string TableLogicalName,
    bool IsSafeToMigrate,
    ValidationSeverityCounts Counts,
    IReadOnlyCollection<EnvironmentComparisonFinding> Findings);
