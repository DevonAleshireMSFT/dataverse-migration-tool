using System.Collections.Concurrent;
using DataverseMigrationTool.Application.Contracts.Solutions;
using DataverseMigrationTool.Application.Ports;

namespace DataverseMigrationTool.Infrastructure.Solutions;

public sealed class InMemorySolutionMigrationRunStore : ISolutionMigrationRunStore
{
    private readonly ConcurrentDictionary<Guid, SolutionMigrationRun> runs = new();

    public Task SaveAsync(SolutionMigrationRun run, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(run);
        cancellationToken.ThrowIfCancellationRequested();
        runs[run.Id] = run;
        return Task.CompletedTask;
    }

    public Task<SolutionMigrationRun?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        runs.TryGetValue(id, out SolutionMigrationRun? run);
        return Task.FromResult(run);
    }
}
