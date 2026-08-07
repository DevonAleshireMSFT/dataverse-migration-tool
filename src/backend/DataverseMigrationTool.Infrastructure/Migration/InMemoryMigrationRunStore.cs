using System.Collections.Concurrent;
using DataverseMigrationTool.Application.Contracts.Migration;
using DataverseMigrationTool.Application.Ports;

namespace DataverseMigrationTool.Infrastructure.Migration;

public sealed class InMemoryMigrationRunStore : IMigrationRunStore
{
    private readonly ConcurrentDictionary<Guid, MigrationRun> runs = new();

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
}
