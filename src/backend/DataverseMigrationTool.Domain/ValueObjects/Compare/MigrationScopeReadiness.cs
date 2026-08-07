namespace DataverseMigrationTool.Domain.ValueObjects.Compare;

public sealed record MigrationScopeReadiness(IReadOnlyCollection<TableMigrationReadiness> Tables)
{
    public IReadOnlyCollection<string> SafeTableLogicalNames =>
        Tables.Where(table => table.IsSafeToMigrate).Select(table => table.TableLogicalName).ToArray();

    public IReadOnlyCollection<string> BlockedTableLogicalNames =>
        Tables.Where(table => !table.IsSafeToMigrate).Select(table => table.TableLogicalName).ToArray();
}
