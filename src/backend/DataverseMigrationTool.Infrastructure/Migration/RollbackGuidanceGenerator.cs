using DataverseMigrationTool.Application.Contracts.Migration;
using DataverseMigrationTool.Application.Ports;

namespace DataverseMigrationTool.Infrastructure.Migration;

public sealed class RollbackGuidanceGenerator : IRollbackGuidanceGenerator
{
    public RollbackGuidance Generate(MigrationRun run, MigrationCheckpoint checkpoint, DateTimeOffset generatedAt)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(checkpoint);

        RollbackArtifactReference runState = new(
            RollbackArtifactKind.RunState,
            run.RunId.ToString("D"),
            "Migration run state with final status, table summaries, and redacted execution errors.");
        RollbackArtifactReference checkpointArtifact = new(
            RollbackArtifactKind.Checkpoint,
            $"{checkpoint.CheckpointId:D}:marker:{checkpoint.Marker}",
            "Checkpoint source of truth for table, batch, source id, target id, retry, and failure context.");
        RollbackArtifactReference operationLog = new(
            RollbackArtifactKind.OperationLog,
            $"migration-job:{run.JobId:D}",
            "Redacted operation log entries for execution, checkpoint, and failure decisions.");
        RollbackArtifactReference validationReport = new(
            RollbackArtifactKind.ValidationReport,
            $"migration-job:{run.JobId:D}:latest",
            "Validation findings that explain blockers, warnings, and preconditions for recovery.");
        RollbackArtifactReference[] artifacts = [runState, checkpointArtifact, operationLog, validationReport];

        RollbackAction[] actions = checkpoint.Tables
            .OrderBy(table => table.TableLogicalName, StringComparer.OrdinalIgnoreCase)
            .SelectMany(table => table.Records
                .OrderBy(record => record.SourceId)
                .Select(record => CreateAction(table.TableLogicalName, record, checkpointArtifact, operationLog, validationReport)))
            .ToArray();

        string summary = actions.Length == 0
            ? "No checkpointed target record mutations were found. Review validation findings and operation logs before rerun or resume."
            : $"Generated rollback guidance for {actions.Length} checkpointed record operations. Delete only records classified as created by this run; updates and unknown upserts need manual recovery review.";

        return new RollbackGuidance(Guid.NewGuid(), run.JobId, run.RunId, generatedAt, summary, actions, artifacts);
    }

    private static RollbackAction CreateAction(
        string tableLogicalName,
        MigrationRecordCheckpoint record,
        RollbackArtifactReference checkpoint,
        RollbackArtifactReference operationLog,
        RollbackArtifactReference validationReport)
    {
        RollbackArtifactReference[] references = [checkpoint, operationLog, validationReport];

        if (record.Status == MigrationCheckpointUnitStatus.Completed && record.TargetId is Guid targetId)
        {
            return record.Disposition switch
            {
                MigrationRecordWriteDisposition.Created => new RollbackAction(
                    tableLogicalName,
                    record.SourceId,
                    targetId,
                    record.Status,
                    record.Disposition,
                    RollbackReversibility.ReversibleViaSupportedApi,
                    "Dataverse Delete",
                    $"Delete target {tableLogicalName} record {targetId:D} through supported Dataverse APIs after confirming no newer dependent data should be preserved.",
                    references),
                MigrationRecordWriteDisposition.Updated => new RollbackAction(
                    tableLogicalName,
                    record.SourceId,
                    targetId,
                    record.Status,
                    record.Disposition,
                    RollbackReversibility.RequiresManualRecovery,
                    "Manual restore; no safe delete",
                    "This run updated a pre-existing target record and did not capture its prior field values. Do not delete it as rollback; restore from backup/source-of-truth or apply a corrective migration after reviewing validation and operation logs.",
                    references),
                _ => new RollbackAction(
                    tableLogicalName,
                    record.SourceId,
                    targetId,
                    record.Status,
                    record.Disposition,
                    RollbackReversibility.ConditionallyReversible,
                    "Conditional Dataverse Delete after audit confirmation",
                    "The checkpoint has a target id but does not prove whether the upsert created or updated the record. Verify Dataverse audit/operation logs before deleting; if the record pre-existed, recover manually instead.",
                    references)
            };
        }

        if (record.Status is MigrationCheckpointUnitStatus.RetryPending or MigrationCheckpointUnitStatus.TerminalFailed)
        {
            return new RollbackAction(
                tableLogicalName,
                record.SourceId,
                record.TargetId,
                record.Status,
                record.Disposition,
                RollbackReversibility.RequiresManualRecovery,
                "Resume or manual investigation",
                "The checkpoint shows an incomplete or failed write. Review the error code, validation findings, and operation logs; resume after correction rather than attempting blind rollback.",
                references);
        }

        return new RollbackAction(
            tableLogicalName,
            record.SourceId,
            record.TargetId,
            record.Status,
            record.Disposition,
            RollbackReversibility.Irreversible,
            "No supported rollback operation",
            "No completed target mutation with a reliable target id was checkpointed. There is no supported API action this tool can safely recommend for rollback.",
            references);
    }
}
