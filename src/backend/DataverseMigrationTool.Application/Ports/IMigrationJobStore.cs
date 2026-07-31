using DataverseMigrationTool.Domain.Entities;

namespace DataverseMigrationTool.Application.Ports;

public interface IMigrationJobStore
{
    Task SaveAsync(MigrationJob job, CancellationToken cancellationToken = default);

    Task<MigrationJob?> FindAsync(Guid jobId, CancellationToken cancellationToken = default);
}

