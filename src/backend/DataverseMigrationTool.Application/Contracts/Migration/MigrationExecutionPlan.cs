namespace DataverseMigrationTool.Application.Contracts.Migration;

public sealed record MigrationExecutionPlan(IReadOnlyList<MigrationTablePlan> Tables)
{
    public IReadOnlyList<string> OrderedTableLogicalNames => Tables.Select(table => table.TableLogicalName).ToArray();
}

public sealed record MigrationTablePlan(
    string TableLogicalName,
    IReadOnlyCollection<string> DependsOnTables,
    bool HasDeferredRelationshipPatches,
    MigrationTableIdempotency Idempotency);

public sealed record MigrationTableIdempotency(
    MigrationIdempotencyMode Mode,
    IReadOnlyList<string> KeyFieldLogicalNames,
    string Guidance)
{
    public bool SupportsSafeResume => Mode is MigrationIdempotencyMode.AlternateKey or MigrationIdempotencyMode.SourceRecordId or MigrationIdempotencyMode.SourceToTargetIdMap;
}

public enum MigrationIdempotencyMode
{
    None,
    SourceRecordId,
    AlternateKey,
    SourceToTargetIdMap
}
