using DataverseMigrationTool.Application.Contracts;
using DataverseMigrationTool.Application.Ports;
using DataverseMigrationTool.Domain.Entities;

namespace DataverseMigrationTool.Infrastructure.Jobs;

public sealed class PlaceholderMigrationEngine(
    IMigrationJobStore jobStore,
    IOperationLogger operationLogger) : IMigrationEngine
{
    public async Task<MigrationJob> CreateJobAsync(
        CreateMigrationJobRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        MigrationJob job = new(
            Guid.NewGuid(),
            request.Source,
            request.Target,
            request.Selection,
            request.Mode);

        await jobStore.SaveAsync(job, cancellationToken);
        await operationLogger.RecordAsync(job.Id, "MigrationJobCreated", "Placeholder migration job created.", cancellationToken);

        return job;
    }

    public Task<MigrationJob?> GetJobAsync(Guid jobId, CancellationToken cancellationToken = default)
        => jobStore.FindAsync(jobId, cancellationToken);
}

