using DataverseMigrationTool.Application.Ports;
using DataverseMigrationTool.Domain.ValueObjects.Compare;
using DataverseMigrationTool.Domain.ValueObjects.Metadata;
using DataverseMigrationTool.Domain.ValueObjects.Validation;

namespace DataverseMigrationTool.Infrastructure.Compare;

public sealed class EnvironmentMetadataComparer : IMetadataSnapshotComparer
{
    private const string MissingTableCode = "DMT-COMPARE-TABLE-MISSING";
    private const string ExtraTableCode = "DMT-COMPARE-TABLE-EXTRA";
    private const string MissingFieldCode = "DMT-COMPARE-FIELD-MISSING";
    private const string ExtraFieldCode = "DMT-COMPARE-FIELD-EXTRA";
    private const string FieldTypeMismatchCode = "DMT-COMPARE-FIELD-TYPE-MISMATCH";
    private const string FieldRequiredLevelMismatchCode = "DMT-COMPARE-FIELD-REQUIREDLEVEL-MISMATCH";
    private const string FieldTargetMismatchCode = "DMT-COMPARE-FIELD-TARGET-MISMATCH";
    private const string MissingRelationshipCode = "DMT-COMPARE-RELATIONSHIP-MISSING";
    private const string RelationshipMismatchCode = "DMT-COMPARE-RELATIONSHIP-MISMATCH";
    private const string MissingAlternateKeyCode = "DMT-COMPARE-ALTERNATEKEY-MISSING";
    private const string AlternateKeyMismatchCode = "DMT-COMPARE-ALTERNATEKEY-MISMATCH";
    private const string MissingChoiceCode = "DMT-COMPARE-CHOICE-MISSING";
    private const string ChoiceKindMismatchCode = "DMT-COMPARE-CHOICE-KIND-MISMATCH";
    private const string MissingChoiceOptionCode = "DMT-COMPARE-CHOICE-OPTION-MISSING";
    private const string ExtraChoiceOptionCode = "DMT-COMPARE-CHOICE-OPTION-EXTRA";
    private const string ChoiceOptionLabelMismatchCode = "DMT-COMPARE-CHOICE-OPTION-LABEL-MISMATCH";

    public EnvironmentComparisonReport Compare(MetadataSnapshot sourceSnapshot, MetadataSnapshot targetSnapshot)
    {
        ArgumentNullException.ThrowIfNull(sourceSnapshot);
        ArgumentNullException.ThrowIfNull(targetSnapshot);

        List<EnvironmentComparisonFinding> findings = [];
        Dictionary<string, TableMetadata> sourceTables = sourceSnapshot.Tables.ToDictionary(
            table => table.LogicalName,
            StringComparer.OrdinalIgnoreCase);
        Dictionary<string, TableMetadata> targetTables = targetSnapshot.Tables.ToDictionary(
            table => table.LogicalName,
            StringComparer.OrdinalIgnoreCase);

        foreach (TableMetadata sourceTable in sourceTables.Values.OrderBy(table => table.LogicalName, StringComparer.OrdinalIgnoreCase))
        {
            if (!targetTables.TryGetValue(sourceTable.LogicalName, out TableMetadata? targetTable))
            {
                findings.Add(new EnvironmentComparisonFinding(
                    MissingTableCode,
                    $"Target environment is missing source table '{sourceTable.LogicalName}'.",
                    ValidationSeverity.Blocker,
                    ComparisonSubjectKind.Table,
                    sourceTable.LogicalName,
                    sourceTable.LogicalName));
                continue;
            }

            CompareFields(sourceTable, targetTable, findings);
            CompareRelationships(sourceTable, targetTable, findings);
            CompareAlternateKeys(sourceTable, targetTable, findings);
        }

        foreach (TableMetadata targetOnlyTable in targetTables.Values
            .Where(table => !sourceTables.ContainsKey(table.LogicalName))
            .OrderBy(table => table.LogicalName, StringComparer.OrdinalIgnoreCase))
        {
            findings.Add(new EnvironmentComparisonFinding(
                ExtraTableCode,
                $"Target environment has table '{targetOnlyTable.LogicalName}' that is not in source.",
                ValidationSeverity.Info,
                ComparisonSubjectKind.Table,
                targetOnlyTable.LogicalName,
                targetOnlyTable.LogicalName));
        }

        CompareChoices(sourceSnapshot.Choices, targetSnapshot.Choices, findings);

        IReadOnlyCollection<EnvironmentComparisonFinding> orderedFindings = findings
            .OrderBy(finding => finding.TableLogicalName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(finding => finding.FieldLogicalName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(finding => finding.Code, StringComparer.OrdinalIgnoreCase)
            .ThenBy(finding => finding.SubjectName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        MigrationScopeReadiness migrationScope = CreateMigrationScope(sourceTables.Values, orderedFindings);

        return new EnvironmentComparisonReport(
            sourceSnapshot.Environment,
            targetSnapshot.Environment,
            DateTimeOffset.UtcNow,
            orderedFindings,
            migrationScope);
    }

    private static void CompareFields(
        TableMetadata sourceTable,
        TableMetadata targetTable,
        ICollection<EnvironmentComparisonFinding> findings)
    {
        Dictionary<string, FieldMetadata> sourceFields = sourceTable.Fields.ToDictionary(
            field => field.LogicalName,
            StringComparer.OrdinalIgnoreCase);
        Dictionary<string, FieldMetadata> targetFields = targetTable.Fields.ToDictionary(
            field => field.LogicalName,
            StringComparer.OrdinalIgnoreCase);

        foreach (FieldMetadata sourceField in sourceFields.Values.OrderBy(field => field.LogicalName, StringComparer.OrdinalIgnoreCase))
        {
            string subjectName = $"{sourceTable.LogicalName}.{sourceField.LogicalName}";
            if (!targetFields.TryGetValue(sourceField.LogicalName, out FieldMetadata? targetField))
            {
                findings.Add(new EnvironmentComparisonFinding(
                    MissingFieldCode,
                    $"Target table '{sourceTable.LogicalName}' is missing source field '{sourceField.LogicalName}'.",
                    ValidationSeverity.Blocker,
                    ComparisonSubjectKind.Field,
                    subjectName,
                    sourceTable.LogicalName,
                    sourceField.LogicalName));
                continue;
            }

            if (sourceField.Type != targetField.Type)
            {
                findings.Add(new EnvironmentComparisonFinding(
                    FieldTypeMismatchCode,
                    $"Field '{subjectName}' type differs. Source is '{sourceField.Type}', target is '{targetField.Type}'.",
                    ValidationSeverity.Blocker,
                    ComparisonSubjectKind.Field,
                    subjectName,
                    sourceTable.LogicalName,
                    sourceField.LogicalName));
            }

            if (sourceField.RequiredLevel != targetField.RequiredLevel)
            {
                ValidationSeverity severity = IsTargetRequiredLevelStricter(sourceField.RequiredLevel, targetField.RequiredLevel)
                    ? ValidationSeverity.Blocker
                    : ValidationSeverity.Warning;
                findings.Add(new EnvironmentComparisonFinding(
                    FieldRequiredLevelMismatchCode,
                    $"Field '{subjectName}' required level differs. Source is '{sourceField.RequiredLevel}', target is '{targetField.RequiredLevel}'.",
                    severity,
                    ComparisonSubjectKind.Field,
                    subjectName,
                    sourceTable.LogicalName,
                    sourceField.LogicalName));
            }

            if (!SetEquals(sourceField.TargetTableLogicalNames, targetField.TargetTableLogicalNames))
            {
                findings.Add(new EnvironmentComparisonFinding(
                    FieldTargetMismatchCode,
                    $"Lookup field '{subjectName}' target tables differ between source and target.",
                    ValidationSeverity.Blocker,
                    ComparisonSubjectKind.Field,
                    subjectName,
                    sourceTable.LogicalName,
                    sourceField.LogicalName));
            }
        }

        foreach (FieldMetadata targetOnlyField in targetFields.Values
            .Where(field => !sourceFields.ContainsKey(field.LogicalName))
            .OrderBy(field => field.LogicalName, StringComparer.OrdinalIgnoreCase))
        {
            findings.Add(new EnvironmentComparisonFinding(
                ExtraFieldCode,
                $"Target table '{targetTable.LogicalName}' has field '{targetOnlyField.LogicalName}' that is not in source.",
                ValidationSeverity.Info,
                ComparisonSubjectKind.Field,
                $"{targetTable.LogicalName}.{targetOnlyField.LogicalName}",
                targetTable.LogicalName,
                targetOnlyField.LogicalName));
        }
    }

    private static void CompareRelationships(
        TableMetadata sourceTable,
        TableMetadata targetTable,
        ICollection<EnvironmentComparisonFinding> findings)
    {
        Dictionary<string, RelationshipMetadata> sourceRelationships = sourceTable.Relationships.ToDictionary(
            RelationshipKey,
            StringComparer.OrdinalIgnoreCase);
        Dictionary<string, RelationshipMetadata> targetRelationships = targetTable.Relationships.ToDictionary(
            RelationshipKey,
            StringComparer.OrdinalIgnoreCase);

        foreach (RelationshipMetadata sourceRelationship in sourceRelationships.Values.OrderBy(RelationshipKey, StringComparer.OrdinalIgnoreCase))
        {
            if (!targetRelationships.TryGetValue(RelationshipKey(sourceRelationship), out RelationshipMetadata? targetRelationship))
            {
                findings.Add(new EnvironmentComparisonFinding(
                    MissingRelationshipCode,
                    $"Target table '{sourceTable.LogicalName}' is missing source relationship '{sourceRelationship.SchemaName}'.",
                    ValidationSeverity.Blocker,
                    ComparisonSubjectKind.Relationship,
                    sourceRelationship.SchemaName,
                    sourceTable.LogicalName));
                continue;
            }

            if (sourceRelationship.Type != targetRelationship.Type
                || !StringComparer.OrdinalIgnoreCase.Equals(sourceRelationship.ReferencingTableLogicalName, targetRelationship.ReferencingTableLogicalName)
                || !StringComparer.OrdinalIgnoreCase.Equals(sourceRelationship.ReferencingFieldLogicalName, targetRelationship.ReferencingFieldLogicalName)
                || !StringComparer.OrdinalIgnoreCase.Equals(sourceRelationship.ReferencedTableLogicalName, targetRelationship.ReferencedTableLogicalName)
                || !StringComparer.OrdinalIgnoreCase.Equals(sourceRelationship.ReferencedFieldLogicalName, targetRelationship.ReferencedFieldLogicalName)
                || !StringComparer.OrdinalIgnoreCase.Equals(sourceRelationship.IntersectTableName, targetRelationship.IntersectTableName))
            {
                findings.Add(new EnvironmentComparisonFinding(
                    RelationshipMismatchCode,
                    $"Relationship '{sourceRelationship.SchemaName}' differs between source and target.",
                    ValidationSeverity.Blocker,
                    ComparisonSubjectKind.Relationship,
                    sourceRelationship.SchemaName,
                    sourceTable.LogicalName));
            }
        }
    }

    private static void CompareAlternateKeys(
        TableMetadata sourceTable,
        TableMetadata targetTable,
        ICollection<EnvironmentComparisonFinding> findings)
    {
        Dictionary<string, AlternateKeyMetadata> sourceKeys = sourceTable.AlternateKeys.ToDictionary(
            key => key.LogicalName,
            StringComparer.OrdinalIgnoreCase);
        Dictionary<string, AlternateKeyMetadata> targetKeys = targetTable.AlternateKeys.ToDictionary(
            key => key.LogicalName,
            StringComparer.OrdinalIgnoreCase);

        foreach (AlternateKeyMetadata sourceKey in sourceKeys.Values.OrderBy(key => key.LogicalName, StringComparer.OrdinalIgnoreCase))
        {
            if (!targetKeys.TryGetValue(sourceKey.LogicalName, out AlternateKeyMetadata? targetKey))
            {
                findings.Add(new EnvironmentComparisonFinding(
                    MissingAlternateKeyCode,
                    $"Target table '{sourceTable.LogicalName}' is missing alternate key '{sourceKey.LogicalName}'.",
                    ValidationSeverity.Warning,
                    ComparisonSubjectKind.AlternateKey,
                    $"{sourceTable.LogicalName}.{sourceKey.LogicalName}",
                    sourceTable.LogicalName));
                continue;
            }

            if (!SetEquals(sourceKey.FieldLogicalNames, targetKey.FieldLogicalNames))
            {
                findings.Add(new EnvironmentComparisonFinding(
                    AlternateKeyMismatchCode,
                    $"Alternate key '{sourceTable.LogicalName}.{sourceKey.LogicalName}' field set differs between source and target.",
                    ValidationSeverity.Warning,
                    ComparisonSubjectKind.AlternateKey,
                    $"{sourceTable.LogicalName}.{sourceKey.LogicalName}",
                    sourceTable.LogicalName));
            }
        }
    }

    private static void CompareChoices(
        IReadOnlyList<ChoiceMetadata> sourceChoices,
        IReadOnlyList<ChoiceMetadata> targetChoices,
        ICollection<EnvironmentComparisonFinding> findings)
    {
        Dictionary<string, ChoiceMetadata> sourceChoiceMap = sourceChoices.ToDictionary(ChoiceKey, StringComparer.OrdinalIgnoreCase);
        Dictionary<string, ChoiceMetadata> targetChoiceMap = targetChoices.ToDictionary(ChoiceKey, StringComparer.OrdinalIgnoreCase);

        foreach (ChoiceMetadata sourceChoice in sourceChoiceMap.Values.OrderBy(ChoiceKey, StringComparer.OrdinalIgnoreCase))
        {
            string choiceKey = ChoiceKey(sourceChoice);
            if (!targetChoiceMap.TryGetValue(choiceKey, out ChoiceMetadata? targetChoice))
            {
                findings.Add(new EnvironmentComparisonFinding(
                    MissingChoiceCode,
                    $"Target environment is missing source choice '{choiceKey}'.",
                    ValidationSeverity.Blocker,
                    ComparisonSubjectKind.Choice,
                    choiceKey,
                    sourceChoice.TableLogicalName,
                    sourceChoice.FieldLogicalName));
                continue;
            }

            if (sourceChoice.Kind != targetChoice.Kind)
            {
                findings.Add(new EnvironmentComparisonFinding(
                    ChoiceKindMismatchCode,
                    $"Choice '{choiceKey}' kind differs. Source is '{sourceChoice.Kind}', target is '{targetChoice.Kind}'.",
                    ValidationSeverity.Blocker,
                    ComparisonSubjectKind.Choice,
                    choiceKey,
                    sourceChoice.TableLogicalName,
                    sourceChoice.FieldLogicalName));
            }

            CompareChoiceOptions(sourceChoice, targetChoice, findings);
        }

        foreach (ChoiceMetadata targetOnlyChoice in targetChoiceMap.Values
            .Where(choice => !sourceChoiceMap.ContainsKey(ChoiceKey(choice)))
            .OrderBy(ChoiceKey, StringComparer.OrdinalIgnoreCase))
        {
            findings.Add(new EnvironmentComparisonFinding(
                "DMT-COMPARE-CHOICE-EXTRA",
                $"Target environment has choice '{ChoiceKey(targetOnlyChoice)}' that is not in source.",
                ValidationSeverity.Info,
                ComparisonSubjectKind.Choice,
                ChoiceKey(targetOnlyChoice),
                targetOnlyChoice.TableLogicalName,
                targetOnlyChoice.FieldLogicalName));
        }
    }

    private static void CompareChoiceOptions(
        ChoiceMetadata sourceChoice,
        ChoiceMetadata targetChoice,
        ICollection<EnvironmentComparisonFinding> findings)
    {
        Dictionary<int, ChoiceOption> sourceOptions = sourceChoice.Options.ToDictionary(option => option.Value);
        Dictionary<int, ChoiceOption> targetOptions = targetChoice.Options.ToDictionary(option => option.Value);
        string choiceKey = ChoiceKey(sourceChoice);

        foreach (ChoiceOption sourceOption in sourceOptions.Values.OrderBy(option => option.Value))
        {
            string subjectName = $"{choiceKey}:{sourceOption.Value}";
            if (!targetOptions.TryGetValue(sourceOption.Value, out ChoiceOption? targetOption))
            {
                findings.Add(new EnvironmentComparisonFinding(
                    MissingChoiceOptionCode,
                    $"Target choice '{choiceKey}' is missing source option value '{sourceOption.Value}'.",
                    ValidationSeverity.Blocker,
                    ComparisonSubjectKind.ChoiceOption,
                    subjectName,
                    sourceChoice.TableLogicalName,
                    sourceChoice.FieldLogicalName));
                continue;
            }

            if (!StringComparer.Ordinal.Equals(sourceOption.Label, targetOption.Label))
            {
                findings.Add(new EnvironmentComparisonFinding(
                    ChoiceOptionLabelMismatchCode,
                    $"Choice option '{subjectName}' label differs. Source is '{sourceOption.Label}', target is '{targetOption.Label}'.",
                    ValidationSeverity.Warning,
                    ComparisonSubjectKind.ChoiceOption,
                    subjectName,
                    sourceChoice.TableLogicalName,
                    sourceChoice.FieldLogicalName));
            }
        }

        foreach (ChoiceOption targetOnlyOption in targetOptions.Values
            .Where(option => !sourceOptions.ContainsKey(option.Value))
            .OrderBy(option => option.Value))
        {
            findings.Add(new EnvironmentComparisonFinding(
                ExtraChoiceOptionCode,
                $"Target choice '{choiceKey}' has option value '{targetOnlyOption.Value}' that is not in source.",
                ValidationSeverity.Info,
                ComparisonSubjectKind.ChoiceOption,
                $"{choiceKey}:{targetOnlyOption.Value}",
                targetChoice.TableLogicalName,
                targetChoice.FieldLogicalName));
        }
    }

    private static MigrationScopeReadiness CreateMigrationScope(
        IEnumerable<TableMetadata> sourceTables,
        IReadOnlyCollection<EnvironmentComparisonFinding> findings)
    {
        TableMigrationReadiness[] tableReadiness = sourceTables
            .OrderBy(table => table.LogicalName, StringComparer.OrdinalIgnoreCase)
            .Select(table =>
            {
                EnvironmentComparisonFinding[] tableFindings = findings
                    .Where(finding => IsFindingForTable(finding, table.LogicalName))
                    .ToArray();
                ValidationSeverityCounts counts = new(
                    tableFindings.Count(finding => finding.Severity == ValidationSeverity.Blocker),
                    tableFindings.Count(finding => finding.Severity == ValidationSeverity.Warning),
                    tableFindings.Count(finding => finding.Severity == ValidationSeverity.Info));

                return new TableMigrationReadiness(
                    table.LogicalName,
                    counts.Blockers == 0,
                    counts,
                    tableFindings);
            })
            .ToArray();

        return new MigrationScopeReadiness(tableReadiness);
    }

    private static bool IsFindingForTable(EnvironmentComparisonFinding finding, string tableLogicalName)
        => StringComparer.OrdinalIgnoreCase.Equals(finding.TableLogicalName, tableLogicalName);

    private static string RelationshipKey(RelationshipMetadata relationship)
        => string.IsNullOrWhiteSpace(relationship.SchemaName)
            ? $"{relationship.Type}:{relationship.ReferencingTableLogicalName}:{relationship.ReferencingFieldLogicalName}:{relationship.ReferencedTableLogicalName}:{relationship.ReferencedFieldLogicalName}:{relationship.IntersectTableName}"
            : relationship.SchemaName;

    private static string ChoiceKey(ChoiceMetadata choice)
        => string.IsNullOrWhiteSpace(choice.TableLogicalName) || string.IsNullOrWhiteSpace(choice.FieldLogicalName)
            ? choice.Name
            : $"{choice.TableLogicalName}.{choice.FieldLogicalName}";

    private static bool IsTargetRequiredLevelStricter(MetadataRequiredLevel source, MetadataRequiredLevel target)
        => RequiredLevelRank(target) > RequiredLevelRank(source);

    private static int RequiredLevelRank(MetadataRequiredLevel level) => level switch
    {
        MetadataRequiredLevel.None => 0,
        MetadataRequiredLevel.Recommended => 1,
        MetadataRequiredLevel.ApplicationRequired => 2,
        MetadataRequiredLevel.SystemRequired => 3,
        _ => 0
    };

    private static bool SetEquals(IEnumerable<string> source, IEnumerable<string> target)
        => new HashSet<string>(source, StringComparer.OrdinalIgnoreCase)
            .SetEquals(target);
}
