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
    public Task<MigrationExecutionResult> ExecuteAsync(
        MigrationJob job,
        MigrationExecutionOptions? options = null,
        IProgress<MigrationExecutionProgress>? progress = null,
        CancellationToken cancellationToken = default)
        => ExecuteCoreAsync(job, resume: false, options, progress, cancellationToken);

    public Task<MigrationExecutionResult> ResumeAsync(
        MigrationJob job,
        MigrationExecutionOptions? options = null,
        IProgress<MigrationExecutionProgress>? progress = null,
        CancellationToken cancellationToken = default)
        => ExecuteCoreAsync(job, resume: true, options, progress, cancellationToken);

    private async Task<MigrationExecutionResult> ExecuteCoreAsync(
        MigrationJob job,
        bool resume,
        MigrationExecutionOptions? options,
        IProgress<MigrationExecutionProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);
        options ??= MigrationExecutionOptions.Default;

        MigrationCheckpoint? existingCheckpoint = resume ? await runStore.FindLatestCheckpointForJobAsync(job.Id, cancellationToken) : null;
        MigrationRun run = await CreateOrResumeRunAsync(job, existingCheckpoint, resume, cancellationToken);
        CheckpointState checkpointState = CheckpointState.From(existingCheckpoint, run.RunId, job.Id);
        List<MigrationTableExecutionSummary> summaries = [];
        List<MigrationExecutionError> errors = checkpointState.Errors.ToList();
        List<DeferredRelationshipPatch> deferredPatches = [];
        MigrationIdMap idMap = new();
        checkpointState.RehydrateIdMap(idMap);

        await MarkAsync(job, run, MigrationJobStatus.Validating, resume ? "ValidatingResume" : "Validating", null, progress, cancellationToken);

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
            await PersistRunAndCheckpointAsync(run, checkpointState, errors, "Validation blockers must be corrected before resume.", MigrationJobStatus.Failed, cancellationToken);
            await MarkAsync(job, run, MigrationJobStatus.Failed, "ValidationBlocked", null, progress, cancellationToken);
            return new MigrationExecutionResult(job.Id, run.RunId, false, summaries, errors);
        }

        await MarkAsync(job, run, MigrationJobStatus.Planning, resume ? "PlanningResume" : "Planning", null, progress, cancellationToken);
        MetadataDiscoveryResult metadataResult = await metadataDiscoveryService.DiscoverAsync(
            new MetadataDiscoveryRequest(job.Source, new MetadataDiscoveryScope(job.Selection.TableLogicalNames), MetadataCachePolicy.Default),
            cancellationToken);
        MigrationExecutionPlan plan = planner.CreatePlan(job.Selection, metadataResult.Snapshot);
        await operationLogger.RecordAsync(job.Id, "MigrationPlanCreated", $"Migration plan contains {plan.Tables.Count} tables; resume={resume}; checkpointMarker={checkpointState.Marker}.", cancellationToken);

        foreach (MigrationTablePlan table in plan.Tables)
        {
            cancellationToken.ThrowIfCancellationRequested();
            MigrationTableCheckpoint? startingTableCheckpoint = checkpointState.GetTable(table.TableLogicalName);
            if (startingTableCheckpoint?.IsCompleted == true)
            {
                summaries.Add(new MigrationTableExecutionSummary(
                    table.TableLogicalName,
                    startingTableCheckpoint.RecordsRead,
                    startingTableCheckpoint.RecordsWritten,
                    startingTableCheckpoint.RecordsSkipped,
                    startingTableCheckpoint.RecordsFailed));
                await operationLogger.RecordAsync(job.Id, "MigrationTableResumeSkipped", $"Table {table.TableLogicalName} already completed at checkpoint marker {checkpointState.Marker}; sourceIds={startingTableCheckpoint.Records.Count}.", cancellationToken);
                continue;
            }

            TableCounters counters = TableCounters.From(table.TableLogicalName, startingTableCheckpoint);
            int batchNumber = startingTableCheckpoint?.LastCompletedBatchNumber ?? 0;
            List<MigrationRecordWriteRequest> batch = [];
            await MarkAsync(job, run, MigrationJobStatus.Extracting, "Extracting", table.TableLogicalName, progress, cancellationToken, counters);

            await foreach (MigrationRecord record in dataProvider.ExtractRecordsAsync(
                new MigrationDataReadRequest(job.Source, table.TableLogicalName, options.BatchSettings.MaxBatchSize),
                cancellationToken))
            {
                if (checkpointState.IsRecordCompleted(table.TableLogicalName, record.SourceId))
                {
                    counters.Skipped++;
                    continue;
                }

                counters.Read++;
                TransformedMigrationRecord transformed = transformer.Transform(record, idMap, table.Idempotency);
                batch.Add(transformed.WriteRequest);
                deferredPatches.AddRange(transformed.DeferredPatches);

                if (batch.Count >= options.BatchSettings.MaxBatchSize)
                {
                    batchNumber++;
                    await FlushBatchAsync(job, run, table, batchNumber, batch, counters, idMap, checkpointState, errors, options, progress, cancellationToken);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                batchNumber++;
                await FlushBatchAsync(job, run, table, batchNumber, batch, counters, idMap, checkpointState, errors, options, progress, cancellationToken);
            }

            MigrationJobStatus tableStatus = counters.Failed == 0 ? MigrationJobStatus.Completed : MigrationJobStatus.Failed;
            checkpointState.UpsertTable(table, counters, tableStatus, batchNumber, lastProcessedKey: null);
            summaries.Add(counters.ToSummary());
            await PersistRunAndCheckpointAsync(run, checkpointState, errors, BuildResumeGuidance(checkpointState, errors), MigrationJobStatus.Running, cancellationToken);
            await operationLogger.RecordAsync(job.Id, "MigrationTableCompleted", $"Table {table.TableLogicalName} completed: read={counters.Read}, written={counters.Written}, skipped={counters.Skipped}, failed={counters.Failed}, idempotency={table.Idempotency.Mode}.", cancellationToken);
        }

        await PatchDeferredRelationshipsAsync(job, run, deferredPatches, idMap, errors, progress, cancellationToken);

        bool succeeded = errors.Count == 0;
        MigrationJobStatus finalStatus = succeeded ? MigrationJobStatus.Completed : MigrationJobStatus.Failed;
        await PersistRunAndCheckpointAsync(run, checkpointState, errors, BuildResumeGuidance(checkpointState, errors), finalStatus, cancellationToken);
        await MarkAsync(job, run, finalStatus, finalStatus.ToString(), null, progress, cancellationToken);

        return new MigrationExecutionResult(job.Id, run.RunId, succeeded, summaries, errors);
    }

    private async Task<MigrationRun> CreateOrResumeRunAsync(MigrationJob job, MigrationCheckpoint? existingCheckpoint, bool resume, CancellationToken cancellationToken)
    {
        if (resume && existingCheckpoint is not null)
        {
            MigrationRun? existingRun = await runStore.FindAsync(existingCheckpoint.RunId, cancellationToken);
            MigrationRun resumedRun = (existingRun ?? new MigrationRun(existingCheckpoint.RunId, job.Id, MigrationJobStatus.Running, DateTimeOffset.UtcNow)) with
            {
                Checkpoint = existingCheckpoint,
                ResumeGuidance = existingCheckpoint.ResumeGuidance,
                Errors = existingCheckpoint.Errors
            };
            await runStore.SaveAsync(resumedRun.WithStatus(MigrationJobStatus.Running, DateTimeOffset.UtcNow), cancellationToken);
            await operationLogger.RecordAsync(job.Id, "MigrationResumeStarted", $"Resume started from checkpoint marker {existingCheckpoint.Marker}; terminalErrors={existingCheckpoint.Errors.Count(error => !error.Retryable)}; retryableErrors={existingCheckpoint.Errors.Count(error => error.Retryable)}.", cancellationToken);
            return resumedRun;
        }

        MigrationRun run = new(Guid.NewGuid(), job.Id, MigrationJobStatus.Validating, DateTimeOffset.UtcNow);
        await runStore.SaveAsync(run, cancellationToken);
        return run;
    }

    private async Task FlushBatchAsync(
        MigrationJob job,
        MigrationRun run,
        MigrationTablePlan table,
        int batchNumber,
        List<MigrationRecordWriteRequest> batch,
        TableCounters counters,
        MigrationIdMap idMap,
        CheckpointState checkpointState,
        List<MigrationExecutionError> errors,
        MigrationExecutionOptions options,
        IProgress<MigrationExecutionProgress>? progress,
        CancellationToken cancellationToken)
    {
        await MarkAsync(job, run, MigrationJobStatus.Loading, "Loading", counters.TableLogicalName, progress, cancellationToken, counters);
        List<MigrationRecordWriteRequest> pending = batch.ToList();
        int attempt = 0;
        int batchFailed = 0;

        while (pending.Count > 0 && attempt <= options.BatchSettings.MaxRetryAttempts)
        {
            attempt++;
            IReadOnlyList<MigrationRecordWriteResult> results = await dataProvider.UpsertBatchAsync(job.Target, pending, cancellationToken);
            List<MigrationRecordWriteRequest> retryableRecords = [];

            foreach (MigrationRecordWriteResult result in results)
            {
                MigrationRecordWriteRequest? original = pending.FirstOrDefault(record => record.SourceId == result.SourceId);
                if (result.Succeeded && result.TargetId is Guid targetId)
                {
                    counters.Written++;
                    if (checkpointState.GetRecord(table.TableLogicalName, result.SourceId)?.Status is MigrationCheckpointUnitStatus.RetryPending or MigrationCheckpointUnitStatus.TerminalFailed && counters.Failed > 0)
                    {
                        counters.Failed--;
                    }

                    errors.RemoveAll(error => error.TableLogicalName.Equals(result.TableLogicalName, StringComparison.OrdinalIgnoreCase) && error.SourceRecordId == result.SourceId);
                    idMap.Record(result.TableLogicalName, result.SourceId, targetId);
                    checkpointState.UpsertRecord(table, result.SourceId, targetId, MigrationCheckpointUnitStatus.Completed, attempt, null);
                    continue;
                }

                MigrationExecutionError error = result.Error ?? new MigrationExecutionError(result.TableLogicalName, result.SourceId, "UnknownWriteFailure", "Dataverse write failed without a provider error.", false, "Review secure server diagnostics and retry after correcting the failed record.", attempt);
                error = error with { Attempt = attempt };
                bool canRetry = error.Retryable && attempt <= options.BatchSettings.MaxRetryAttempts;
                if (canRetry && original is not null)
                {
                    retryableRecords.Add(original);
                    checkpointState.UpsertRecord(table, result.SourceId, null, MigrationCheckpointUnitStatus.RetryPending, attempt, error.Code);
                    continue;
                }

                counters.Failed++;
                batchFailed++;
                errors.Add(error with
                {
                    Retryable = error.Retryable && attempt <= options.BatchSettings.MaxRetryAttempts,
                    OperatorAction = error.Retryable
                        ? "Retry attempts are exhausted. Resume after fixing the transient cause; completed records will be skipped."
                        : error.OperatorAction
                });
                checkpointState.UpsertRecord(table, result.SourceId, null, MigrationCheckpointUnitStatus.TerminalFailed, attempt, error.Code);
            }

            pending = retryableRecords;
        }

        checkpointState.UpsertBatch(table, counters, batchNumber, batch.Count, batchFailed, attempt);
        checkpointState.UpsertTable(table, counters, MigrationJobStatus.Running, batchNumber, batch.LastOrDefault()?.SourceId.ToString("D"));
        await PersistRunAndCheckpointAsync(run, checkpointState, errors, BuildResumeGuidance(checkpointState, errors), MigrationJobStatus.Running, cancellationToken);
        await operationLogger.RecordAsync(job.Id, "MigrationBatchCheckpointed", $"Table {counters.TableLogicalName} batch={batchNumber} checkpointed: size={batch.Count}, attempt={attempt}, written={counters.Written}, failed={counters.Failed}, marker={checkpointState.Marker}.", cancellationToken);
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

    private async Task PersistRunAndCheckpointAsync(MigrationRun run, CheckpointState checkpointState, List<MigrationExecutionError> errors, string guidance, MigrationJobStatus status, CancellationToken cancellationToken)
    {
        MigrationCheckpoint checkpoint = checkpointState.ToCheckpoint(errors, guidance);
        MigrationRun updatedRun = (run with
        {
            Tables = checkpoint.Tables.Select(table => new MigrationRunTableState(table.TableLogicalName, table.Status, table.RecordsRead, table.RecordsWritten, table.RecordsSkipped, table.RecordsFailed, table.LastCompletedBatchNumber)).ToArray(),
            Errors = errors.ToArray(),
            Checkpoint = checkpoint,
            ResumeGuidance = guidance
        }).WithStatus(status, DateTimeOffset.UtcNow);

        await runStore.SaveAsync(updatedRun, cancellationToken);
        await runStore.SaveCheckpointAsync(checkpoint, cancellationToken);
        checkpointState.Marker = checkpoint.Marker;
    }

    private static string BuildResumeGuidance(CheckpointState checkpointState, IReadOnlyList<MigrationExecutionError> errors)
    {
        MigrationExecutionError? terminal = errors.LastOrDefault(error => !error.Retryable);
        if (terminal is not null)
        {
            return $"Terminal failure in table {terminal.TableLogicalName}; sourceId={terminal.SourceRecordId?.ToString("D") ?? "none"}; code={terminal.Code}. Correct the cause and resume. Completed checkpointed records will be skipped.";
        }

        MigrationExecutionError? retryable = errors.LastOrDefault(error => error.Retryable);
        if (retryable is not null)
        {
            return $"Retryable failures remain in table {retryable.TableLogicalName}; sourceId={retryable.SourceRecordId?.ToString("D") ?? "none"}; code={retryable.Code}. Resume will retry incomplete records up to the configured cap and skip completed records.";
        }

        return $"Checkpoint marker {checkpointState.Marker} is safe to resume. Completed tables, batches, and source ids will be skipped; at most the active batch may repeat.";
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

        public static TableCounters From(string tableLogicalName, MigrationTableCheckpoint? checkpoint) => new(tableLogicalName)
        {
            Read = checkpoint?.RecordsRead ?? 0,
            Written = checkpoint?.RecordsWritten ?? 0,
            Skipped = checkpoint?.RecordsSkipped ?? 0,
            Failed = checkpoint?.RecordsFailed ?? 0
        };

        public MigrationTableExecutionSummary ToSummary() => new(TableLogicalName, Read, Written, Skipped, Failed);
    }

    private sealed class CheckpointState
    {
        private readonly Dictionary<string, MigrationTableCheckpoint> tables;
        private readonly Dictionary<(string Table, Guid SourceId), MigrationRecordCheckpoint> records;
        private readonly Dictionary<(string Table, int Batch), MigrationBatchCheckpoint> batches;
        private readonly Guid checkpointId;
        private readonly Guid runId;
        private readonly Guid jobId;

        private CheckpointState(Guid checkpointId, Guid runId, Guid jobId, long marker, IReadOnlyList<MigrationExecutionError> errors, Dictionary<string, MigrationTableCheckpoint> tables)
        {
            this.checkpointId = checkpointId;
            this.runId = runId;
            this.jobId = jobId;
            Marker = marker;
            Errors = errors;
            this.tables = tables;
            records = tables.Values
                .SelectMany(table => table.Records.Select(record => (table.TableLogicalName, record)))
                .ToDictionary(pair => (Normalize(pair.TableLogicalName), pair.record.SourceId), pair => pair.record);
            batches = tables.Values
                .SelectMany(table => table.Batches.Select(batch => (table.TableLogicalName, batch)))
                .ToDictionary(pair => (Normalize(pair.TableLogicalName), pair.batch.BatchNumber), pair => pair.batch);
        }

        public long Marker { get; set; }

        public IReadOnlyList<MigrationExecutionError> Errors { get; }

        public static CheckpointState From(MigrationCheckpoint? checkpoint, Guid runId, Guid jobId)
        {
            if (checkpoint is null)
            {
                return new CheckpointState(Guid.NewGuid(), runId, jobId, 0, Array.Empty<MigrationExecutionError>(), new Dictionary<string, MigrationTableCheckpoint>(StringComparer.OrdinalIgnoreCase));
            }

            return new CheckpointState(
                checkpoint.CheckpointId,
                checkpoint.RunId,
                checkpoint.JobId,
                checkpoint.Marker,
                checkpoint.Errors,
                checkpoint.Tables.ToDictionary(table => table.TableLogicalName, StringComparer.OrdinalIgnoreCase));
        }

        public MigrationTableCheckpoint? GetTable(string tableLogicalName)
            => tables.GetValueOrDefault(tableLogicalName);

        public bool IsRecordCompleted(string tableLogicalName, Guid sourceId)
            => records.TryGetValue((Normalize(tableLogicalName), sourceId), out MigrationRecordCheckpoint? record) && record.Status == MigrationCheckpointUnitStatus.Completed;

        public MigrationRecordCheckpoint? GetRecord(string tableLogicalName, Guid sourceId)
            => records.GetValueOrDefault((Normalize(tableLogicalName), sourceId));

        public void RehydrateIdMap(MigrationIdMap idMap)
        {
            foreach (KeyValuePair<(string Table, Guid SourceId), MigrationRecordCheckpoint> pair in records.Where(pair => pair.Value.TargetId is not null && pair.Value.Status == MigrationCheckpointUnitStatus.Completed))
            {
                idMap.Record(pair.Key.Table, pair.Key.SourceId, pair.Value.TargetId!.Value);
            }
        }

        public void UpsertRecord(MigrationTablePlan table, Guid sourceId, Guid? targetId, MigrationCheckpointUnitStatus status, int attempt, string? errorCode)
        {
            records[(Normalize(table.TableLogicalName), sourceId)] = new MigrationRecordCheckpoint(sourceId, targetId, status, attempt, errorCode);
        }

        public void UpsertBatch(MigrationTablePlan table, TableCounters counters, int batchNumber, int batchSize, int failed, int attempt)
        {
            batches[(Normalize(table.TableLogicalName), batchNumber)] = new MigrationBatchCheckpoint(
                batchNumber,
                failed == 0 ? MigrationCheckpointUnitStatus.Completed : MigrationCheckpointUnitStatus.RetryPending,
                attempt,
                batchSize,
                batchSize - failed,
                0,
                failed);
        }

        public void UpsertTable(MigrationTablePlan table, TableCounters counters, MigrationJobStatus status, int batchNumber, string? lastProcessedKey)
        {
            string normalized = Normalize(table.TableLogicalName);
            MigrationBatchCheckpoint[] tableBatches = batches
                .Where(pair => pair.Key.Table == normalized)
                .Select(pair => pair.Value)
                .OrderBy(batch => batch.BatchNumber)
                .ToArray();
            MigrationRecordCheckpoint[] tableRecords = records
                .Where(pair => pair.Key.Table == normalized)
                .Select(pair => pair.Value)
                .OrderBy(record => record.SourceId)
                .ToArray();

            tables[table.TableLogicalName] = new MigrationTableCheckpoint(
                table.TableLogicalName,
                status,
                table.Idempotency,
                counters.Read,
                counters.Written,
                counters.Skipped,
                counters.Failed,
                batchNumber,
                counters.Read + counters.Skipped,
                lastProcessedKey,
                null,
                tableBatches,
                tableRecords);
        }

        public MigrationCheckpoint ToCheckpoint(IReadOnlyList<MigrationExecutionError> errors, string guidance)
        {
            return new MigrationCheckpoint(checkpointId, runId, jobId, Marker, DateTimeOffset.UtcNow).Advance(
                tables.Values.OrderBy(table => table.TableLogicalName, StringComparer.OrdinalIgnoreCase).ToArray(),
                errors.ToArray(),
                guidance,
                DateTimeOffset.UtcNow);
        }

        private static string Normalize(string tableLogicalName) => tableLogicalName.ToUpperInvariant();
    }
}
