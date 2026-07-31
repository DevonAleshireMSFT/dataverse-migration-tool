using DataverseMigrationTool.Application.Contracts;
using DataverseMigrationTool.Domain.Entities;

namespace DataverseMigrationTool.Application.Ports;

public interface IMigrationEngine
{
    Task<MigrationJob> CreateJobAsync(
        CreateMigrationJobRequest request,
        CancellationToken cancellationToken = default);

    Task<MigrationJob?> GetJobAsync(
        Guid jobId,
        CancellationToken cancellationToken = default);
}

