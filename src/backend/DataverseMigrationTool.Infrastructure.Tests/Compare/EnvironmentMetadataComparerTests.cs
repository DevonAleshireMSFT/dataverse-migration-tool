using DataverseMigrationTool.Domain.Enums;
using DataverseMigrationTool.Domain.ValueObjects;
using DataverseMigrationTool.Domain.ValueObjects.Compare;
using DataverseMigrationTool.Domain.ValueObjects.Metadata;
using DataverseMigrationTool.Domain.ValueObjects.Validation;
using DataverseMigrationTool.Infrastructure.Compare;

namespace DataverseMigrationTool.Infrastructure.Tests.Compare;

public sealed class EnvironmentMetadataComparerTests
{
    private readonly EnvironmentMetadataComparer comparer = new();

    [Fact]
    public void CompareMissingSourceTableProducesBlockerAndBlocksScopeTable()
    {
        MetadataSnapshot source = Snapshot([Table("account")]);
        MetadataSnapshot target = Snapshot([]);

        EnvironmentComparisonReport report = comparer.Compare(source, target);

        EnvironmentComparisonFinding finding = Assert.Single(report.Blockers);
        Assert.Equal("DMT-COMPARE-TABLE-MISSING", finding.Code);
        Assert.Equal(ValidationSeverity.Blocker, finding.Severity);
        Assert.True(report.IsBlocked);
        Assert.Equal(["account"], report.MigrationScope.BlockedTableLogicalNames);
        Assert.Empty(report.MigrationScope.SafeTableLogicalNames);
    }

    [Fact]
    public void CompareExtraTargetTableProducesInfoWithoutBlocking()
    {
        MetadataSnapshot source = Snapshot([Table("account")]);
        MetadataSnapshot target = Snapshot([Table("account"), Table("contact")]);

        EnvironmentComparisonReport report = comparer.Compare(source, target);

        EnvironmentComparisonFinding finding = Assert.Single(report.Information);
        Assert.Equal("DMT-COMPARE-TABLE-EXTRA", finding.Code);
        Assert.True(report.IsReady);
        Assert.Equal(["account"], report.MigrationScope.SafeTableLogicalNames);
    }

    [Fact]
    public void CompareMissingFieldProducesBlocker()
    {
        MetadataSnapshot source = Snapshot([Table("account", fields: [Field("name")])]);
        MetadataSnapshot target = Snapshot([Table("account")]);

        EnvironmentComparisonReport report = comparer.Compare(source, target);

        EnvironmentComparisonFinding finding = Assert.Single(report.Blockers);
        Assert.Equal("DMT-COMPARE-FIELD-MISSING", finding.Code);
        Assert.Equal("account.name", finding.SubjectName);
        Assert.Equal(["account"], report.MigrationScope.BlockedTableLogicalNames);
    }

    [Fact]
    public void CompareFieldTypeMismatchProducesBlocker()
    {
        MetadataSnapshot source = Snapshot([Table("account", fields: [Field("revenue", MetadataFieldType.Money)])]);
        MetadataSnapshot target = Snapshot([Table("account", fields: [Field("revenue", MetadataFieldType.Decimal)])]);

        EnvironmentComparisonReport report = comparer.Compare(source, target);

        EnvironmentComparisonFinding finding = Assert.Single(report.Blockers);
        Assert.Equal("DMT-COMPARE-FIELD-TYPE-MISMATCH", finding.Code);
        Assert.Contains("Money", finding.Message, StringComparison.Ordinal);
        Assert.Contains("Decimal", finding.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CompareTargetStricterRequiredLevelProducesBlockerAndLooserProducesWarning()
    {
        MetadataSnapshot source = Snapshot([Table("account", fields:
        [
            Field("optionalcode", requiredLevel: MetadataRequiredLevel.None),
            Field("requiredcode", requiredLevel: MetadataRequiredLevel.ApplicationRequired)
        ])]);
        MetadataSnapshot target = Snapshot([Table("account", fields:
        [
            Field("optionalcode", requiredLevel: MetadataRequiredLevel.ApplicationRequired),
            Field("requiredcode", requiredLevel: MetadataRequiredLevel.None)
        ])]);

        EnvironmentComparisonReport report = comparer.Compare(source, target);

        Assert.Contains(report.Blockers, finding =>
            finding.Code == "DMT-COMPARE-FIELD-REQUIREDLEVEL-MISMATCH"
            && finding.FieldLogicalName == "optionalcode");
        Assert.Contains(report.Warnings, finding =>
            finding.Code == "DMT-COMPARE-FIELD-REQUIREDLEVEL-MISMATCH"
            && finding.FieldLogicalName == "requiredcode");
    }

    [Fact]
    public void CompareRelationshipGapProducesBlocker()
    {
        RelationshipMetadata relationship = new(
            "account_primary_contact",
            MetadataRelationshipType.ManyToOne,
            "account",
            "primarycontactid",
            "contact",
            "contactid",
            IntersectTableName: null,
            IsCustomRelationship: false);
        MetadataSnapshot source = Snapshot([Table("account", relationships: [relationship])]);
        MetadataSnapshot target = Snapshot([Table("account")]);

        EnvironmentComparisonReport report = comparer.Compare(source, target);

        EnvironmentComparisonFinding finding = Assert.Single(report.Blockers);
        Assert.Equal("DMT-COMPARE-RELATIONSHIP-MISSING", finding.Code);
        Assert.Equal("account_primary_contact", finding.SubjectName);
    }

    [Fact]
    public void CompareAlternateKeyGapProducesWarning()
    {
        AlternateKeyMetadata key = new("ak_accountnumber", "ak_accountnumber", "Account Number", ["accountnumber"], IsManaged: false);
        MetadataSnapshot source = Snapshot([Table("account", alternateKeys: [key])]);
        MetadataSnapshot target = Snapshot([Table("account")]);

        EnvironmentComparisonReport report = comparer.Compare(source, target);

        EnvironmentComparisonFinding finding = Assert.Single(report.Warnings);
        Assert.Equal("DMT-COMPARE-ALTERNATEKEY-MISSING", finding.Code);
        Assert.True(report.IsReady);
    }

    [Fact]
    public void CompareChoiceOptionDifferencesSeparateBlockersWarningsAndInfo()
    {
        ChoiceMetadata sourceChoice = new(
            "account_categorycode",
            "Category",
            ChoiceKind.Local,
            [
                new ChoiceOption(1, "Preferred"),
                new ChoiceOption(2, "Standard")
            ],
            "account",
            "categorycode");
        ChoiceMetadata targetChoice = new(
            "account_categorycode",
            "Category",
            ChoiceKind.Local,
            [
                new ChoiceOption(1, "Preferred Customer"),
                new ChoiceOption(3, "Strategic")
            ],
            "account",
            "categorycode");

        MetadataSnapshot source = Snapshot([Table("account", fields: [Field("categorycode", MetadataFieldType.Picklist)])], [sourceChoice]);
        MetadataSnapshot target = Snapshot([Table("account", fields: [Field("categorycode", MetadataFieldType.Picklist)])], [targetChoice]);

        EnvironmentComparisonReport report = comparer.Compare(source, target);

        Assert.Contains(report.Blockers, finding => finding.Code == "DMT-COMPARE-CHOICE-OPTION-MISSING");
        Assert.Contains(report.Warnings, finding => finding.Code == "DMT-COMPARE-CHOICE-OPTION-LABEL-MISMATCH");
        Assert.Contains(report.Information, finding => finding.Code == "DMT-COMPARE-CHOICE-OPTION-EXTRA");
        Assert.Equal(1, report.Counts.Blockers);
        Assert.Equal(1, report.Counts.Warnings);
        Assert.Equal(1, report.Counts.Infos);
    }

    [Fact]
    public void CompareReadinessPassesWithWarningsAndFailsWithBlockers()
    {
        MetadataSnapshot warningSource = Snapshot([Table("account", alternateKeys:
        [
            new AlternateKeyMetadata("ak_accountnumber", "ak_accountnumber", "Account Number", ["accountnumber"], IsManaged: false)
        ])]);
        MetadataSnapshot warningTarget = Snapshot([Table("account")]);

        EnvironmentComparisonReport warningReport = comparer.Compare(warningSource, warningTarget);

        Assert.True(warningReport.IsReady);
        Assert.False(warningReport.IsBlocked);

        MetadataSnapshot blockerTarget = Snapshot([]);
        EnvironmentComparisonReport blockerReport = comparer.Compare(warningSource, blockerTarget);

        Assert.False(blockerReport.IsReady);
        Assert.True(blockerReport.IsBlocked);
    }

    [Fact]
    public void CompareScopeSelectionMarksOnlyBlockerFreeTablesSafe()
    {
        MetadataSnapshot source = Snapshot(
        [
            Table("account", fields: [Field("name")]),
            Table("contact")
        ]);
        MetadataSnapshot target = Snapshot([Table("account"), Table("contact")]);

        EnvironmentComparisonReport report = comparer.Compare(source, target);

        Assert.Equal(["contact"], report.MigrationScope.SafeTableLogicalNames);
        Assert.Equal(["account"], report.MigrationScope.BlockedTableLogicalNames);
    }

    private static MetadataSnapshot Snapshot(
        IReadOnlyList<TableMetadata> tables,
        IReadOnlyList<ChoiceMetadata>? choices = null) => new(
            Environment("DEV"),
            MetadataDiscoveryScope.All,
            DateTimeOffset.Parse("2026-08-06T00:00:00+00:00"),
            tables,
            choices ?? []);

    private static EnvironmentProfile Environment(string name) => new(
        name,
        new Uri($"https://{name.ToLowerInvariant()}.crm.dynamics.com"),
        Guid.Parse("11111111-1111-1111-1111-111111111111"),
        DataverseCloud.Public);

    private static TableMetadata Table(
        string logicalName,
        IReadOnlyList<FieldMetadata>? fields = null,
        IReadOnlyList<RelationshipMetadata>? relationships = null,
        IReadOnlyList<AlternateKeyMetadata>? alternateKeys = null) => new(
            logicalName,
            logicalName,
            logicalName,
            Description: null,
            IsCustomTable: false,
            IsActivity: false,
            IsIntersect: false,
            fields ?? [],
            relationships ?? [],
            alternateKeys ?? []);

    private static FieldMetadata Field(
        string logicalName,
        MetadataFieldType type = MetadataFieldType.String,
        MetadataRequiredLevel requiredLevel = MetadataRequiredLevel.None,
        IReadOnlyCollection<string>? targetTableLogicalNames = null) => new(
            logicalName,
            logicalName,
            logicalName,
            Description: null,
            type,
            requiredLevel,
            IsPrimaryId: false,
            IsPrimaryName: false,
            IsValidForRead: true,
            IsValidForCreate: true,
            IsValidForUpdate: true,
            targetTableLogicalNames ?? []);
}
