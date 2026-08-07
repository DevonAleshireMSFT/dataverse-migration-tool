using DataverseMigrationTool.Application.Contracts.Solutions;

namespace DataverseMigrationTool.Application.Ports;

public interface ISolutionMigrationRunStore
{
    Task SaveAsync(SolutionMigrationRun run, CancellationToken cancellationToken = default);

    Task<SolutionMigrationRun?> FindAsync(Guid id, CancellationToken cancellationToken = default);
}
