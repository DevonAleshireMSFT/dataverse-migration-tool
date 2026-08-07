using DataverseMigrationTool.Domain.ValueObjects.Validation;

namespace DataverseMigrationTool.Domain.Tests.Validation;

public sealed class ValidationReportTests
{
    [Fact]
    public void EmptyReportPassesWithZeroCounts()
    {
        ValidationReport report = ValidationReport.Empty;

        Assert.True(report.Passed);
        Assert.False(report.Failed);
        Assert.Equal(0, report.Counts.Total);
        Assert.Empty(report.Blockers);
        Assert.Empty(report.Warnings);
        Assert.Empty(report.Information);
    }

    [Fact]
    public void ReportFailsWhenAnyBlockerExists()
    {
        ValidationReport report = ValidationReport.FromFindings(
        [
            new ValidationFinding("DMT-TEST-001", "Unsupported component.", ValidationSeverity.Blocker, "Components"),
            new ValidationFinding("DMT-TEST-002", "Optional table missing.", ValidationSeverity.Warning, "Metadata"),
            new ValidationFinding("DMT-TEST-003", "Connectivity checked.", ValidationSeverity.Info, "Connectivity")
        ]);

        Assert.False(report.Passed);
        Assert.True(report.Failed);
        Assert.Single(report.Blockers);
        Assert.Single(report.Warnings);
        Assert.Single(report.Information);
    }

    [Fact]
    public void SeverityCountsSeparateBlockersWarningsAndInfo()
    {
        ValidationReport report = ValidationReport.FromFindings(
        [
            new ValidationFinding("DMT-TEST-001", "First blocker.", ValidationSeverity.Blocker, "Connectivity"),
            new ValidationFinding("DMT-TEST-002", "Second blocker.", ValidationSeverity.Blocker, "Metadata"),
            new ValidationFinding("DMT-TEST-003", "Warning.", ValidationSeverity.Warning, "Metadata"),
            new ValidationFinding("DMT-TEST-004", "Info.", ValidationSeverity.Info, "Metadata")
        ]);

        Assert.Equal(2, report.Counts.Blockers);
        Assert.Equal(1, report.Counts.Warnings);
        Assert.Equal(1, report.Counts.Infos);
        Assert.Equal(4, report.Counts.Total);
        Assert.Equal(2, report.Counts.For(ValidationSeverity.Blocker));
    }

    [Fact]
    public void FindingRequiresStableRuleIdMessageAndCategory()
    {
        Assert.Throws<ArgumentException>(
            () => new ValidationFinding("", "Message.", ValidationSeverity.Info, "Category"));
        Assert.Throws<ArgumentException>(
            () => new ValidationFinding("DMT-TEST-001", "", ValidationSeverity.Info, "Category"));
        Assert.Throws<ArgumentException>(
            () => new ValidationFinding("DMT-TEST-001", "Message.", ValidationSeverity.Info, ""));
    }
}
