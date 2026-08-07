using DataverseMigrationTool.Application.Contracts.Metadata;
using DataverseMigrationTool.Application.Contracts.Migration;
using DataverseMigrationTool.Application.Ports;
using DataverseMigrationTool.Domain.Entities;
using DataverseMigrationTool.Domain.Enums;
using DataverseMigrationTool.Domain.ValueObjects.Metadata;
using DataverseMigrationTool.Domain.ValueObjects.Validation;

namespace DataverseMigrationTool.Infrastructure.Migration;

public sealed class MigrationExecutor(
    IValidationEngine validationEngine,
    IMetadataDiscoveryService metadataDiscoveryService,
    IMigrationDataProvider dataProvider,
    IMigrationRunStore runStore,
    IMigrationJobStore jobStore,
    IOperationLogger operationLogger,
    MigrationExecutionPlanner planner,
    MigrationRecordTransformer transformer) : IMigrationExecutor
{
    public async Task<MigrationExecutionResult> ExecuteAsync(
        MigrationJob job,
        MigrationExecutionOptions? options = null,
        IProgress<MigrationExecutionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        options ??= MigrationExecutionOptions.Default;

        MigrationRun run = new(Guid.NewGuid(), job.Id, MigrationJobStatus.Validating, DateTimeOffset.UtcNow);
        List<MigrationTableExecutionSummary> summaries = [];
        List<MigrationExecutionError> errors = [];
        List<DeferredRelationshipPatch> deferredPatches = [];
        MigrationIdMap idMap = new();

        await runStore.SaveAsync(run, cancellationToken);
        await MarkAsync(job, run, MigrationJobStatus.Validating, "Validating", null, progress, cancellationToken);

        ValidationReport validationReport = await validationEngine.ValidateAsync(job, cancellationToken);
        if (validationReport.Blockers.Count > 0)
        {
            errors.AddRange(validationReport.Blockers.Select(blocker => new MigrationExecutionError(
                blocker.Target ?? "migration",
                null,
                blocker.Code,
                "Validation blocker stopped migration execution.",
                false,
                blocker.Message,
                0)));
            await runStore.SaveAsync((run with { Errors = errors.ToArray() }).WithStatus(MigrationJobStatus.Failed, DateTimeOffset.UtcNow), cancellationToken);
            await MarkAsync(job, run, MigrationJobStatus.Failed, "ValidationBlocked", null, progress, cancellationToken);
            return new MigrationExecutionResult(job.Id, run.RunId, false, summaries, errors);
        }

        await MarkAsync(job, run, MigrationJobStatus.Planning, "Planning", null, progress, cancellationToken);
        MetadataDiscoveryResult metadataResult = await metadataDiscoveryService.DiscoverAsync(
            new MetadataDiscoveryRequest(job.Source, new MetadataDiscoveryScope(job.Selection.TableLogicalNames), MetadataCachePolicy.Default),
            cancellationToken);
        MigrationExecutionPlan plan = planner.CreatePlan(job.Selection, metadataResult.Snapshot);
        await operationLogger.RecordAsync(job.Id, "MigrationPlanCreated", $"Migration plan contains {plan.Tables.Count} tables.", cancellationToken);

        foreach (MigrationTablePlan table in plan.Tables)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TableCounters counters = new(table.TableLogicalName);
            await MarkAsync(job, run, MigrationJobStatus.Extracting, "Extracting", table.TableLogicalName, progress, cancellationToken);
            List<MigrationRecordWriteRequest> batch = [];

            await foreach (MigrationRecord record in dataProvider.ExtractRecordsAsync(
                new MigrationDataReadRequest(job.Source, table.TableLogicalName, options.BatchSettings.MaxBatchSize),
                cancellationToken))
            {
                counters.Read++;
                TransformedMigrationRecord transformed = transformer.Transform(record, idMap);
                batch.Add(transformed.WriteRequest);
                deferredPatches.AddRange(transformed.DeferredPatches);

                if (batch.Count >= options.BatchSettings.MaxBatchSize)
                {
                    await FlushBatchAsync(job, run, batch, counters, idMap, errors, options, progress, cancellationToken);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                await FlushBatchAsync(job, run, batch, counters, idMap, errors, options, progress, cancellationToken);
            }

            summaries.Add(counters.ToSummary());
            run = run with
            {
                Tables = summaries.Select(summary => new MigrationRunTableState(summary.TableLogicalName, MigrationJobStatus.Completed, summary.RecordsRead, summary.RecordsWritten, summary.RecordsSkipped, summary.RecordsFailed, 0)).ToArray(),
                Errors = errors.ToArray()
            };
            await runStore.SaveAsync(run, cancellationToken);
            await operationLogger.RecordAsync(job.Id, "MigrationTableCompleted", $"Table {table.TableLogicalName} completed: read={counters.Read}, written={counters.Written}, failed={counters.Failed}.", cancellationToken);
        }

        await PatchDeferredRelationshipsAsync(job, run, deferredPatches, idMap, errors, progress, cancellationToken);

        bool succeeded = errors.Count == 0;
        MigrationJobStatus finalStatus = succeeded ? MigrationJobStatus.Completed : MigrationJobStatus.Failed;
        run = run with
        {
            Tables = summaries.Select(summary => new MigrationRunTableState(summary.TableLogicalName, finalStatus, summary.RecordsRead, summary.RecordsWritten, summary.RecordsSkipped, summary.RecordsFailed, 0)).ToArray(),
            Errors = errors.ToArray()
        };
        await runStore.SaveAsync(run.WithStatus(finalStatus, DateTimeOffset.UtcNow), cancellationToken);
        await MarkAsync(job, run, finalStatus, finalStatus.ToString(), null, progress, cancellationToken);

        return new MigrationExecutionResult(job.Id, run.RunId, succeeded, summaries, errors);
    }

    private async Task FlushBatchAsync(
        MigrationJob job,
        MigrationRun run,
        List<MigrationRecordWriteRequest> batch,
        TableCounters counters,
        MigrationIdMap idMap,
        List<MigrationExecutionError> errors,
        MigrationExecutionOptions options,
        IProgress<MigrationExecutionProgress>? progress,
        CancellationToken cancellationToken)
    {
        await MarkAsync(job, run, MigrationJobStatus.Loading, "Loading", counters.TableLogicalName, progress, cancellationToken, counters);
        IReadOnlyList<MigrationRecordWriteResult> results = await dataProvider.UpsertBatchAsync(job.Target, batch, cancellationToken);
        List<MigrationRecordWriteRequest> retryableRecords = [];

        foreach (MigrationRecordWriteResult result in results)
        {
            if (result.Succeeded && result.TargetId is Guid targetId)
            {
                counters.Written++;
                idMap.Record(result.TableLogicalName, result.SourceId, targetId);
                continue;
            }

            counters.Failed++;
            if (result.Error is not null)
            {
                if (result.Error.Retryable)
                {
                    MigrationRecordWriteRequest? original = batch.FirstOrDefault(record => record.SourceId == result.SourceId);
                    if (original is not null)
                    {
                        retryableRecords.Add(original);
                    }
                }
                else
                {
                    errors.Add(result.Error);
                }
            }
        }

        if (retryableRecords.Count > 0 && options.BatchSettings.MaxRetryAttempts == 0)
        {
            foreach (MigrationRecordWriteRequest retryableRecord in retryableRecords)
            {
                errors.Add(new MigrationExecutionError(retryableRecord.TableLogicalName, retryableRecord.SourceId, "RetryRequired", "Record failed with a retryable error and retry attempts are disabled.", true, "Retry the failed batch.", 0));
            }
        }

        if (retryableRecords.Count > 0 && options.BatchSettings.MaxRetryAttempts > 0)
        {
            IReadOnlyList<MigrationRecordWriteResult> retryResults = await dataProvider.UpsertBatchAsync(job.Target, retryableRecords, cancellationToken);
            foreach (MigrationRecordWriteResult result in retryResults)
            {
                if (result.Succeeded && result.TargetId is Guid targetId)
                {
                    counters.Written++;
                    counters.Failed--;
                    idMap.Record(result.TableLogicalName, result.SourceId, targetId);
                }
                else if (result.Error is not null)
                {
                    errors.Add(result.Error with { Attempt = result.Error.Attempt == 0 ? 1 : result.Error.Attempt });
                }
            }
        }

        await operationLogger.RecordAsync(job.Id, "MigrationBatchLoaded", $"Table {counters.TableLogicalName} batch loaded: size={batch.Count}, written={counters.Written}, failed={counters.Failed}.", cancellationToken);
    }

    private async Task PatchDeferredRelationshipsAsync(
        MigrationJob job,
        MigrationRun run,
        List<DeferredRelationshipPatch> deferredPatches,
        MigrationIdMap idMap,
        List<MigrationExecutionError> errors,
        IProgress<MigrationExecutionProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (deferredPatches.Count == 0)
        {
            return;
        }

        await MarkAsync(job, run, MigrationJobStatus.PatchingRelationships, "PatchingRelationships", null, progress, cancellationToken);
        List<MigrationRelationshipPatchRequest> patchRequests = [];
        foreach (DeferredRelationshipPatch patch in deferredPatches)
        {
            if (!idMap.TryGetTargetId(patch.TableLogicalName, patch.SourceId, out Guid recordTargetId) ||
                !idMap.TryGetTargetId(patch.TargetTableLogicalName, patch.SourceTargetId, out Guid lookupTargetId))
            {
                errors.Add(new MigrationExecutionError(
                    patch.TableLogicalName,
                    patch.SourceId,
                    "DeferredRelationshipTargetMissing",
                    "Deferred relationship target was not available after loading records.",
                    false,
                    "Check source selection and failed records, then retry after correcting missing parent or related records.",
                    0));
                continue;
            }

            patchRequests.Add(new MigrationRelationshipPatchRequest(
                patch.TableLogicalName,
                recordTargetId,
                patch.FieldLogicalName,
                new MigrationTargetLookupValue(patch.TargetTableLogicalName, lookupTargetId)));
        }

        if (patchRequests.Count > 0)
        {
            errors.AddRange(await dataProvider.PatchRelationshipsAsync(job.Target, patchRequests, cancellationToken));
        }

        await operationLogger.RecordAsync(job.Id, "MigrationRelationshipsPatched", $"Deferred relationship patch pass completed: patches={patchRequests.Count}, errors={errors.Count}.", cancellationToken);
    }

    private async Task MarkAsync(
        MigrationJob job,
        MigrationRun run,
        MigrationJobStatus status,
        string stage,
        string? tableLogicalName,
        IProgress<MigrationExecutionProgress>? progress,
        CancellationToken cancellationToken,
        TableCounters? counters = null)
    {
        job.MarkStatus(status);
        await jobStore.SaveAsync(job, cancellationToken);
        await operationLogger.RecordAsync(job.Id, "MigrationStatusChanged", $"Status={status}; stage={stage}; table={tableLogicalName ?? "none"}.", cancellationToken);
        progress?.Report(new MigrationExecutionProgress(
            job.Id,
            run.RunId,
            status,
            stage,
            tableLogicalName,
            counters?.Read ?? 0,
            counters?.Written ?? 0,
            counters?.Skipped ?? 0,
            counters?.Failed ?? 0));
    }

    private sealed class TableCounters(string tableLogicalName)
    {
        public string TableLogicalName { get; } = tableLogicalName;

        public int Read { get; set; }

        public int Written { get; set; }

        public int Skipped { get; set; }

        public int Failed { get; set; }

        public MigrationTableExecutionSummary ToSummary() => new(TableLogicalName, Read, Written, Skipped, Failed);
    }
}
