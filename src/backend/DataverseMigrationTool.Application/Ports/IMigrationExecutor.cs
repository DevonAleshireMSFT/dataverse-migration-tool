using DataverseMigrationTool.Application.Contracts.Migration;
using DataverseMigrationTool.Domain.Entities;

namespace DataverseMigrationTool.Application.Ports;

public interface IMigrationExecutor
{
    Task<MigrationExecutionResult> ExecuteAsync(
        MigrationJob job,
        MigrationExecutionOptions? options = null,
        IProgress<MigrationExecutionProgress>? progress = null,
        CancellationToken cancellationToken = default);

    Task<MigrationExecutionResult> ResumeAsync(
        MigrationJob job,
        MigrationExecutionOptions? options = null,
        IProgress<MigrationExecutionProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
