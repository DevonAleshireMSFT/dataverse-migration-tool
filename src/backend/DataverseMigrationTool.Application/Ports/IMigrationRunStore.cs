using DataverseMigrationTool.Application.Contracts.Migration;

namespace DataverseMigrationTool.Application.Ports;

public interface IMigrationRunStore
{
    Task SaveAsync(MigrationRun run, CancellationToken cancellationToken = default);

    Task<MigrationRun?> FindAsync(Guid runId, CancellationToken cancellationToken = default);

    Task<MigrationRun?> FindLatestForJobAsync(Guid jobId, CancellationToken cancellationToken = default);

    Task SaveCheckpointAsync(MigrationCheckpoint checkpoint, CancellationToken cancellationToken = default);

    Task<MigrationCheckpoint?> FindLatestCheckpointForJobAsync(Guid jobId, CancellationToken cancellationToken = default);

    Task SaveRollbackGuidanceAsync(RollbackGuidance guidance, CancellationToken cancellationToken = default);

    Task<RollbackGuidance?> FindLatestRollbackGuidanceForJobAsync(Guid jobId, CancellationToken cancellationToken = default);
}
