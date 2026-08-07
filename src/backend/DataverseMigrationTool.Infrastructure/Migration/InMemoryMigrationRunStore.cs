using System.Collections.Concurrent;
using DataverseMigrationTool.Application.Contracts.Migration;
using DataverseMigrationTool.Application.Ports;

namespace DataverseMigrationTool.Infrastructure.Migration;

public sealed class InMemoryMigrationRunStore : IMigrationRunStore
{
    private readonly ConcurrentDictionary<Guid, MigrationRun> runs = new();
    private readonly ConcurrentDictionary<Guid, MigrationCheckpoint> checkpoints = new();
    private readonly ConcurrentDictionary<Guid, RollbackGuidance> rollbackGuidance = new();

    public Task SaveAsync(MigrationRun run, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(run);
        runs[run.RunId] = run;
        return Task.CompletedTask;
    }

    public Task<MigrationRun?> FindAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        runs.TryGetValue(runId, out MigrationRun? run);
        return Task.FromResult(run);
    }

    public Task<MigrationRun?> FindLatestForJobAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        MigrationRun? run = runs.Values
            .Where(candidate => candidate.JobId == jobId)
            .OrderByDescending(candidate => candidate.StartedAt)
            .FirstOrDefault();

        return Task.FromResult(run);
    }

    public Task SaveCheckpointAsync(MigrationCheckpoint checkpoint, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(checkpoint);
        checkpoints[checkpoint.CheckpointId] = checkpoint;
        if (runs.TryGetValue(checkpoint.RunId, out MigrationRun? run))
        {
            runs[run.RunId] = run with { Checkpoint = checkpoint, ResumeGuidance = checkpoint.ResumeGuidance, Errors = checkpoint.Errors };
        }

        return Task.CompletedTask;
    }

    public Task<MigrationCheckpoint?> FindLatestCheckpointForJobAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        MigrationCheckpoint? checkpoint = checkpoints.Values
            .Where(candidate => candidate.JobId == jobId)
            .OrderByDescending(candidate => candidate.Marker)
            .ThenByDescending(candidate => candidate.UpdatedAt)
            .FirstOrDefault();

        return Task.FromResult(checkpoint);
    }

    public Task SaveRollbackGuidanceAsync(RollbackGuidance guidance, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(guidance);
        rollbackGuidance[guidance.GuidanceId] = guidance;
        return Task.CompletedTask;
    }

    public Task<RollbackGuidance?> FindLatestRollbackGuidanceForJobAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        RollbackGuidance? guidance = rollbackGuidance.Values
            .Where(candidate => candidate.JobId == jobId)
            .OrderByDescending(candidate => candidate.GeneratedAt)
            .FirstOrDefault();

        return Task.FromResult(guidance);
    }
}
