namespace DataverseMigrationTool.Application.Contracts.Migration;

public sealed record MigrationExecutionOptions(MigrationBatchSettings BatchSettings)
{
    public static MigrationExecutionOptions Default { get; } = new(new MigrationBatchSettings());
}
