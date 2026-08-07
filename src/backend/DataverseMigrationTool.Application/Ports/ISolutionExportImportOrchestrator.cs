using DataverseMigrationTool.Application.Contracts.Solutions;

namespace DataverseMigrationTool.Application.Ports;

public interface ISolutionExportImportOrchestrator
{
    Task<SolutionMigrationRun> StartAsync(
        SolutionMigrationRequest request,
        CancellationToken cancellationToken = default);

    Task<SolutionMigrationRun?> GetAsync(
        Guid migrationId,
        CancellationToken cancellationToken = default);
}
