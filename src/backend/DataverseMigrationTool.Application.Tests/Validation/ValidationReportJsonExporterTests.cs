using DataverseMigrationTool.Application.Contracts.Validation;
using DataverseMigrationTool.Domain.ValueObjects.Validation;

namespace DataverseMigrationTool.Application.Tests.Validation;

public sealed class ValidationReportJsonExporterTests
{
    [Fact]
    public void ExportUsesJsonFriendlyShapeAndStringSeverity()
    {
        ValidationReport report = ValidationReport.FromFindings(
        [
            new ValidationFinding(
                "DMT-TEST-001",
                "A blocker for operators.",
                ValidationSeverity.Blocker,
                "Connectivity",
                "source:DEV")
        ]);

        string json = ValidationReportJsonExporter.Export(report, writeIndented: false);

        Assert.Contains("\"findings\"", json);
        Assert.Contains("\"ruleId\":\"DMT-TEST-001\"", json);
        Assert.Contains("\"severity\":\"Blocker\"", json);
        Assert.Contains("\"counts\"", json);
        Assert.Contains("\"passed\":false", json);
    }

    [Fact]
    public void ImportRoundTripsReportFindings()
    {
        ValidationReport report = ValidationReport.FromFindings(
        [
            new ValidationFinding(
                "DMT-TEST-002",
                "A warning for operators.",
                ValidationSeverity.Warning,
                "Metadata",
                "table:account")
        ]);

        string json = ValidationReportJsonExporter.Export(report, writeIndented: false);
        ValidationReport roundTripped = ValidationReportJsonExporter.Import(json);

        ValidationFinding finding = Assert.Single(roundTripped.Findings);
        Assert.True(roundTripped.Passed);
        Assert.Equal("DMT-TEST-002", finding.RuleId);
        Assert.Equal(ValidationSeverity.Warning, finding.Severity);
        Assert.Equal("table:account", finding.Target);
    }
}
