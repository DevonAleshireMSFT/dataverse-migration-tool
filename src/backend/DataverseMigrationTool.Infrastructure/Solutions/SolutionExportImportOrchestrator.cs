using DataverseMigrationTool.Application.Contracts.Solutions;
using DataverseMigrationTool.Application.Ports;
using DataverseMigrationTool.Domain.ValueObjects.Validation;

namespace DataverseMigrationTool.Infrastructure.Solutions;

public sealed class SolutionExportImportOrchestrator(
    ISolutionAlmProvider almProvider,
    ISolutionMigrationRunStore runStore,
    IOperationLogger operationLogger,
    SupportedSolutionPreflightPolicy preflightPolicy) : ISolutionExportImportOrchestrator
{
    public async Task<SolutionMigrationRun> StartAsync(
        SolutionMigrationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Guid id = Guid.NewGuid();
        DateTimeOffset startedAt = DateTimeOffset.UtcNow;
        SolutionMigrationRun run = new(id, request, SolutionMigrationWorkflowStatus.Validating, ValidationReport.Empty, StartedAt: startedAt);
        await SaveAndLogAsync(run, "SolutionMigrationStarted", $"Solution migration started for '{request.SourceSolutionUniqueName}' as {request.ExportMode}.", cancellationToken);

        try
        {
            SolutionSelectionResult selection = await WithRetryAfterAsync(
                () => almProvider.EnsureUnmanagedSourceSolutionAsync(request, cancellationToken),
                cancellationToken);
            if (!selection.IsUnmanaged)
            {
                ValidationReport selectionReport = ValidationReport.FromFindings([
                    new ValidationFinding(
                        "DMT-SOLUTION-020",
                        $"Source solution '{selection.SolutionUniqueName}' must be unmanaged before export/import orchestration.",
                        ValidationSeverity.Blocker,
                        "Solution ALM",
                        selection.SolutionUniqueName)
                ]);
                run = await BlockAsync(run, selectionReport, cancellationToken);
                return run;
            }

            ValidationReport policyReport = preflightPolicy.Evaluate(request);
            ValidationReport providerReport = await WithRetryAfterAsync(
                () => almProvider.ValidateDependenciesAndTargetReadinessAsync(request, cancellationToken),
                cancellationToken);
            ValidationReport validationReport = Merge(policyReport, providerReport);
            run = run with { ValidationReport = validationReport };
            await runStore.SaveAsync(run, cancellationToken);

            if (validationReport.Failed)
            {
                return await BlockAsync(run, validationReport, cancellationToken);
            }

            run = run with { Status = SolutionMigrationWorkflowStatus.Exporting };
            await SaveAndLogAsync(run, "SolutionExportStarted", $"Exporting '{request.SourceSolutionUniqueName}' as {request.ExportMode} artifact.", cancellationToken);
            SolutionExportResult export = await WithRetryAfterAsync(
                () => almProvider.ExportSolutionAsync(request, cancellationToken),
                cancellationToken);

            run = run with { Status = SolutionMigrationWorkflowStatus.Importing, Export = export };
            await SaveAndLogAsync(run, "SolutionImportStarted", $"Importing {export.Mode} artifact '{export.ArtifactName}' to target '{request.Target.Name}'.", cancellationToken);
            SolutionImportStartResult importStart = await WithRetryAfterAsync(
                () => almProvider.ImportSolutionAsync(request, export, cancellationToken),
                cancellationToken);

            SolutionImportJobStatus importStatus = new(
                importStart.ImportJobId,
                SolutionImportStatus.Running,
                0,
                Array.Empty<SolutionImportDiagnostic>(),
                DateTimeOffset.UtcNow);

            int attempt = 0;
            while (!importStatus.IsTerminal && attempt < Math.Max(1, request.MaxImportPollAttempts))
            {
                cancellationToken.ThrowIfCancellationRequested();
                attempt++;
                importStatus = await WithRetryAfterAsync(
                    () => almProvider.GetImportJobStatusAsync(request, importStart.ImportJobId, cancellationToken),
                    cancellationToken);
                run = run with { ImportJob = importStatus };
                await runStore.SaveAsync(run, cancellationToken);

                if (!importStatus.IsTerminal && request.ImportPollIntervalMilliseconds > 0)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(request.ImportPollIntervalMilliseconds), cancellationToken);
                }
            }

            if (!importStatus.IsTerminal)
            {
                run = run with
                {
                    Status = SolutionMigrationWorkflowStatus.Failed,
                    ImportJob = importStatus,
                    CompletedAt = DateTimeOffset.UtcNow,
                    FailureMessage = "ImportJob polling did not reach a terminal status within the configured attempt limit."
                };
                await SaveAndLogAsync(run, "SolutionImportPollingTimedOut", run.FailureMessage, cancellationToken);
                return run;
            }

            run = run with
            {
                Status = importStatus.Status == SolutionImportStatus.Succeeded ? SolutionMigrationWorkflowStatus.Completed : SolutionMigrationWorkflowStatus.Failed,
                ImportJob = importStatus,
                CompletedAt = DateTimeOffset.UtcNow,
                FailureMessage = importStatus.Status == SolutionImportStatus.Failed ? "Solution import failed; review ImportJob diagnostics." : null
            };
            await SaveAndLogAsync(run, "SolutionMigrationCompleted", $"Solution migration completed with status {run.Status}; importJobId={importStatus.ImportJobId}.", cancellationToken);
            return run;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            run = run with
            {
                Status = SolutionMigrationWorkflowStatus.Failed,
                CompletedAt = DateTimeOffset.UtcNow,
                FailureMessage = ex.Message
            };
            await SaveAndLogAsync(run, "SolutionMigrationFailed", "Solution migration failed; details are available in the returned failure message.", cancellationToken);
            return run;
        }
    }

    public Task<SolutionMigrationRun?> GetAsync(Guid migrationId, CancellationToken cancellationToken = default) =>
        runStore.FindAsync(migrationId, cancellationToken);

    private async Task<SolutionMigrationRun> BlockAsync(SolutionMigrationRun run, ValidationReport report, CancellationToken cancellationToken)
    {
        SolutionMigrationRun blocked = run with
        {
            Status = SolutionMigrationWorkflowStatus.PreflightBlocked,
            ValidationReport = report,
            CompletedAt = DateTimeOffset.UtcNow,
            FailureMessage = "Solution migration preflight produced blocker findings."
        };
        await SaveAndLogAsync(blocked, "SolutionMigrationPreflightBlocked", blocked.FailureMessage, cancellationToken);
        return blocked;
    }

    private async Task SaveAndLogAsync(SolutionMigrationRun run, string operation, string message, CancellationToken cancellationToken)
    {
        await runStore.SaveAsync(run, cancellationToken);
        await operationLogger.RecordAsync(run.Id, operation, message, cancellationToken);
    }

    private static ValidationReport Merge(params ValidationReport[] reports) =>
        ValidationReport.FromFindings(reports.SelectMany(report => report.Findings));

    private static async Task<T> WithRetryAfterAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)
    {
        int attempt = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return await action();
            }
            catch (SolutionAlmTransientException ex) when (attempt < 3)
            {
                attempt++;
                TimeSpan delay = ex.RetryAfter.GetValueOrDefault(TimeSpan.FromSeconds(1));
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, cancellationToken);
                }
            }
        }
    }
}
