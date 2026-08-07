using DataverseMigrationTool.Application.Contracts.Migration;
using DataverseMigrationTool.Domain.ValueObjects;
using DataverseMigrationTool.Domain.ValueObjects.Metadata;

namespace DataverseMigrationTool.Infrastructure.Migration;

public sealed class MigrationExecutionPlanner
{
    public MigrationExecutionPlan CreatePlan(ComponentSelection selection, MetadataSnapshot metadata)
    {
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(metadata);

        string[] selectedTables = ResolveSelectedTables(selection, metadata);
        Dictionary<string, HashSet<string>> dependencies = selectedTables.ToDictionary(
            table => table,
            _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase);
        HashSet<string> deferredTables = new(StringComparer.OrdinalIgnoreCase);

        foreach (TableMetadata table in metadata.Tables.Where(table => dependencies.ContainsKey(table.LogicalName)))
        {
            foreach (RelationshipMetadata relationship in table.Relationships)
            {
                AddDependency(relationship, dependencies, deferredTables);
            }
        }

        Dictionary<string, MigrationTableIdempotency> idempotencyByTable = metadata.Tables
            .Where(table => dependencies.ContainsKey(table.LogicalName))
            .ToDictionary(table => table.LogicalName, ResolveIdempotency, StringComparer.OrdinalIgnoreCase);

        return new MigrationExecutionPlan(TopologicallySort(dependencies, deferredTables, idempotencyByTable));
    }

    private static string[] ResolveSelectedTables(ComponentSelection selection, MetadataSnapshot metadata)
    {
        string[] selected = selection.TableLogicalNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return selected.Length == 0 && selection.IncludeData
            ? metadata.Tables.Select(table => table.LogicalName).ToArray()
            : selected;
    }

    private static void AddDependency(
        RelationshipMetadata relationship,
        Dictionary<string, HashSet<string>> dependencies,
        HashSet<string> deferredTables)
    {
        bool referencesSelected = dependencies.ContainsKey(relationship.ReferencingTableLogicalName);
        bool referencedSelected = dependencies.ContainsKey(relationship.ReferencedTableLogicalName);
        if (!referencesSelected || !referencedSelected)
        {
            return;
        }

        if (relationship.ReferencingTableLogicalName.Equals(relationship.ReferencedTableLogicalName, StringComparison.OrdinalIgnoreCase))
        {
            deferredTables.Add(relationship.ReferencingTableLogicalName);
            return;
        }

        if (relationship.Type is MetadataRelationshipType.ManyToMany && !string.IsNullOrWhiteSpace(relationship.IntersectTableName))
        {
            if (dependencies.ContainsKey(relationship.IntersectTableName))
            {
                dependencies[relationship.IntersectTableName].Add(relationship.ReferencingTableLogicalName);
                dependencies[relationship.IntersectTableName].Add(relationship.ReferencedTableLogicalName);
            }

            return;
        }

        dependencies[relationship.ReferencingTableLogicalName].Add(relationship.ReferencedTableLogicalName);
    }

    private static List<MigrationTablePlan> TopologicallySort(
        Dictionary<string, HashSet<string>> dependencies,
        HashSet<string> deferredTables,
        Dictionary<string, MigrationTableIdempotency> idempotencyByTable)
    {
        Dictionary<string, HashSet<string>> remaining = dependencies.ToDictionary(
            pair => pair.Key,
            pair => new HashSet<string>(pair.Value, StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase);
        List<MigrationTablePlan> ordered = [];

        while (remaining.Count > 0)
        {
            string? next = remaining
                .Where(pair => pair.Value.Count == 0)
                .Select(pair => pair.Key)
                .Order(StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();

            if (next is null)
            {
                next = remaining
                    .OrderBy(pair => pair.Value.Count)
                    .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                    .First()
                    .Key;
                deferredTables.Add(next);
                remaining[next].Clear();
            }

            IReadOnlyCollection<string> tableDependencies = dependencies[next].Order(StringComparer.OrdinalIgnoreCase).ToArray();
            ordered.Add(new MigrationTablePlan(next, tableDependencies, deferredTables.Contains(next), idempotencyByTable[next]));
            remaining.Remove(next);

            foreach (HashSet<string> dependencySet in remaining.Values)
            {
                dependencySet.Remove(next);
            }
        }

        return ordered;
    }

    private static MigrationTableIdempotency ResolveIdempotency(TableMetadata table)
    {
        AlternateKeyMetadata? alternateKey = table.AlternateKeys
            .Where(key => key.FieldLogicalNames.Count > 0)
            .OrderBy(key => key.LogicalName, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        if (alternateKey is not null)
        {
            return new MigrationTableIdempotency(
                MigrationIdempotencyMode.AlternateKey,
                alternateKey.FieldLogicalNames.ToArray(),
                $"Uses alternate key {alternateKey.LogicalName}; completed source ids and source-to-target mappings are checkpointed for resume.");
        }

        return new MigrationTableIdempotency(
            MigrationIdempotencyMode.SourceRecordId,
            Array.Empty<string>(),
            "No alternate key discovered; writes use deterministic source record ids where Dataverse permits it, and completed mappings are checkpointed. Re-run risk is at-least-once if the target cannot honor source ids.");
    }
}
