using DataverseMigrationTool.Application.Contracts.Compare;
using DataverseMigrationTool.Domain.Enums;
using DataverseMigrationTool.Domain.ValueObjects;
using DataverseMigrationTool.Domain.ValueObjects.Compare;
using DataverseMigrationTool.Domain.ValueObjects.Validation;

namespace DataverseMigrationTool.Application.Tests.Compare;

public sealed class EnvironmentComparisonJsonExporterTests
{
    [Fact]
    public void ExportRoundTripsReportForUiAndExports()
    {
        EnvironmentProfile source = Environment("Source");
        EnvironmentProfile target = Environment("Target");
        EnvironmentComparisonFinding finding = new(
            "DMT-COMPARE-FIELD-MISSING",
            "Target is missing field.",
            ValidationSeverity.Blocker,
            ComparisonSubjectKind.Field,
            "account.name",
            "account",
            "name");
        TableMigrationReadiness readiness = new(
            "account",
            IsSafeToMigrate: false,
            new ValidationSeverityCounts(1, 0, 0),
            [finding]);
        EnvironmentComparisonReport report = new(
            source,
            target,
            DateTimeOffset.Parse("2026-08-06T00:00:00+00:00"),
            [finding],
            new MigrationScopeReadiness([readiness]));

        string json = EnvironmentComparisonJsonExporter.Export(report);
        EnvironmentComparisonReport? roundTripped = EnvironmentComparisonJsonExporter.Import(json);

        Assert.NotNull(roundTripped);
        Assert.True(roundTripped.IsBlocked);
        Assert.Equal("Blocker", json.Contains("\"severity\": \"Blocker\"", StringComparison.Ordinal)
            ? "Blocker"
            : string.Empty);
        Assert.Equal("account", roundTripped.MigrationScope.BlockedTableLogicalNames.Single());
        Assert.Equal("DMT-COMPARE-FIELD-MISSING", roundTripped.Findings.Single().Code);
    }

    private static EnvironmentProfile Environment(string name) => new(
        name,
        new Uri($"https://{name.ToLowerInvariant()}.crm.dynamics.com"),
        Guid.Parse("11111111-1111-1111-1111-111111111111"),
        DataverseCloud.Public);
}
