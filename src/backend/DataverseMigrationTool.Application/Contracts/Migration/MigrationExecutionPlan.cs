namespace DataverseMigrationTool.Application.Contracts.Migration;

public sealed record MigrationExecutionPlan(IReadOnlyList<MigrationTablePlan> Tables)
{
    public IReadOnlyList<string> OrderedTableLogicalNames => Tables.Select(table => table.TableLogicalName).ToArray();
}

public sealed record MigrationTablePlan(
    string TableLogicalName,
    IReadOnlyCollection<string> DependsOnTables,
    bool HasDeferredRelationshipPatches);
