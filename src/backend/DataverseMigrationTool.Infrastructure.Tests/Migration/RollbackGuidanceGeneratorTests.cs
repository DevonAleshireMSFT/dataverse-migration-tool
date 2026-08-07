using DataverseMigrationTool.Application.Contracts.Migration;
using DataverseMigrationTool.Domain.Enums;
using DataverseMigrationTool.Infrastructure.Migration;

namespace DataverseMigrationTool.Infrastructure.Tests.Migration;

public sealed class RollbackGuidanceGeneratorTests
{
    [Fact]
    public void Generate_classifies_created_records_as_supported_delete()
    {
        Guid targetId = Guid.NewGuid();
        RollbackGuidance guidance = Generate(new MigrationRecordCheckpoint(
            Guid.NewGuid(),
            targetId,
            MigrationCheckpointUnitStatus.Completed,
            Attempt: 1,
            ErrorCode: null,
            MigrationRecordWriteDisposition.Created));

        RollbackAction action = Assert.Single(guidance.Actions);
        Assert.Equal(RollbackReversibility.ReversibleViaSupportedApi, action.Reversibility);
        Assert.Equal("Dataverse Delete", action.SupportedApiOperation);
        Assert.Contains(targetId.ToString("D"), action.RecommendedOperatorAction);
    }

    [Fact]
    public void Generate_classifies_updated_records_as_manual_recovery()
    {
        RollbackGuidance guidance = Generate(new MigrationRecordCheckpoint(
            Guid.NewGuid(),
            Guid.NewGuid(),
            MigrationCheckpointUnitStatus.Completed,
            Attempt: 1,
            ErrorCode: null,
            MigrationRecordWriteDisposition.Updated));

        RollbackAction action = Assert.Single(guidance.Actions);
        Assert.Equal(RollbackReversibility.RequiresManualRecovery, action.Reversibility);
        Assert.Contains("did not capture its prior field values", action.RecommendedOperatorAction);
        Assert.Contains("Do not delete", action.RecommendedOperatorAction);
    }

    [Fact]
    public void Generate_makes_unknown_upserts_conditional_and_failed_records_manual()
    {
        RollbackGuidance guidance = Generate(
            new MigrationRecordCheckpoint(Guid.NewGuid(), Guid.NewGuid(), MigrationCheckpointUnitStatus.Completed, Attempt: 1, ErrorCode: null),
            new MigrationRecordCheckpoint(Guid.NewGuid(), null, MigrationCheckpointUnitStatus.TerminalFailed, Attempt: 2, ErrorCode: "Validation"));

        Assert.Contains(guidance.Actions, action => action.Reversibility == RollbackReversibility.ConditionallyReversible);
        Assert.Contains(guidance.Actions, action => action.Reversibility == RollbackReversibility.RequiresManualRecovery);
        Assert.Contains(guidance.Actions, action => action.RecommendedOperatorAction.Contains("does not prove whether the upsert created or updated", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Generate_marks_skipped_or_unidentified_records_as_irreversible()
    {
        RollbackGuidance guidance = Generate(new MigrationRecordCheckpoint(
            Guid.NewGuid(),
            null,
            MigrationCheckpointUnitStatus.Skipped,
            Attempt: 0,
            ErrorCode: null));

        RollbackAction action = Assert.Single(guidance.Actions);
        Assert.Equal(RollbackReversibility.Irreversible, action.Reversibility);
        Assert.Equal("No supported rollback operation", action.SupportedApiOperation);
    }

    [Fact]
    public void Generate_references_checkpoint_validation_and_operation_log_artifacts()
    {
        RollbackGuidance guidance = Generate(new MigrationRecordCheckpoint(
            Guid.NewGuid(),
            Guid.NewGuid(),
            MigrationCheckpointUnitStatus.Completed,
            Attempt: 1,
            ErrorCode: null,
            MigrationRecordWriteDisposition.Created));

        Assert.Contains(guidance.ArtifactReferences, artifact => artifact.Kind == RollbackArtifactKind.Checkpoint);
        Assert.Contains(guidance.ArtifactReferences, artifact => artifact.Kind == RollbackArtifactKind.ValidationReport);
        Assert.Contains(guidance.ArtifactReferences, artifact => artifact.Kind == RollbackArtifactKind.OperationLog);
        RollbackAction action = Assert.Single(guidance.Actions);
        Assert.Contains(action.ArtifactReferences, artifact => artifact.Kind == RollbackArtifactKind.Checkpoint);
        Assert.Contains(action.ArtifactReferences, artifact => artifact.Kind == RollbackArtifactKind.ValidationReport);
        Assert.Contains(action.ArtifactReferences, artifact => artifact.Kind == RollbackArtifactKind.OperationLog);
    }

    private static RollbackGuidance Generate(params MigrationRecordCheckpoint[] records)
    {
        Guid runId = Guid.NewGuid();
        Guid jobId = Guid.NewGuid();
        MigrationRun run = new(runId, jobId, MigrationJobStatus.Completed, DateTimeOffset.UtcNow);
        MigrationCheckpoint checkpoint = new MigrationCheckpoint(Guid.NewGuid(), runId, jobId, 3, DateTimeOffset.UtcNow).Advance(
            [
                new MigrationTableCheckpoint(
                    "account",
                    MigrationJobStatus.Completed,
                    new MigrationTableIdempotency(MigrationIdempotencyMode.AlternateKey, ["accountnumber"], "Uses alternate key."),
                    RecordsRead: records.Length,
                    RecordsWritten: records.Count(record => record.Status == MigrationCheckpointUnitStatus.Completed),
                    RecordsSkipped: records.Count(record => record.Status == MigrationCheckpointUnitStatus.Skipped),
                    RecordsFailed: records.Count(record => record.Status == MigrationCheckpointUnitStatus.TerminalFailed),
                    LastCompletedBatchNumber: 1,
                    LastProcessedOffset: records.Length,
                    LastProcessedKey: null,
                    DeltaToken: null,
                    Batches: [new MigrationBatchCheckpoint(1, MigrationCheckpointUnitStatus.Completed, 1, records.Length, records.Length, 0, 0)],
                    Records: records)
            ],
            Array.Empty<MigrationExecutionError>(),
            "Completed.",
            DateTimeOffset.UtcNow);

        return new RollbackGuidanceGenerator().Generate(run, checkpoint, DateTimeOffset.UtcNow);
    }
}
