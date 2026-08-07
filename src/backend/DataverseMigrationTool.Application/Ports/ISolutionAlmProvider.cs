using DataverseMigrationTool.Application.Contracts.Solutions;
using DataverseMigrationTool.Domain.ValueObjects.Validation;

namespace DataverseMigrationTool.Application.Ports;

public interface ISolutionAlmProvider
{
    Task<SolutionSelectionResult> EnsureUnmanagedSourceSolutionAsync(
        SolutionMigrationRequest request,
        CancellationToken cancellationToken = default);

    Task<ValidationReport> ValidateDependenciesAndTargetReadinessAsync(
        SolutionMigrationRequest request,
        CancellationToken cancellationToken = default);

    Task<SolutionExportResult> ExportSolutionAsync(
        SolutionMigrationRequest request,
        CancellationToken cancellationToken = default);

    Task<SolutionImportStartResult> ImportSolutionAsync(
        SolutionMigrationRequest request,
        SolutionExportResult export,
        CancellationToken cancellationToken = default);

    Task<SolutionImportJobStatus> GetImportJobStatusAsync(
        SolutionMigrationRequest request,
        Guid importJobId,
        CancellationToken cancellationToken = default);
}
