using System.Collections.Concurrent;
using DataverseMigrationTool.Application.Ports;
using DataverseMigrationTool.Domain.Entities;

namespace DataverseMigrationTool.Infrastructure.Jobs;

public sealed class InMemoryMigrationJobStore : IMigrationJobStore
{
    private readonly ConcurrentDictionary<Guid, MigrationJob> jobs = new();

    public Task SaveAsync(MigrationJob job, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        jobs[job.Id] = job;
        return Task.CompletedTask;
    }

    public Task<MigrationJob?> FindAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        jobs.TryGetValue(jobId, out MigrationJob? job);
        return Task.FromResult(job);
    }
}

