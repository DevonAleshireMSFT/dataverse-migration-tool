using DataverseMigrationTool.Domain.Enums;

namespace DataverseMigrationTool.Application.Contracts.Migration;

public sealed record MigrationCheckpoint
{
    public MigrationCheckpoint(Guid checkpointId, Guid runId, Guid jobId, long marker, DateTimeOffset updatedAt)
    {
        CheckpointId = checkpointId == Guid.Empty ? throw new ArgumentException("Checkpoint id must not be empty.", nameof(checkpointId)) : checkpointId;
        RunId = runId == Guid.Empty ? throw new ArgumentException("Run id must not be empty.", nameof(runId)) : runId;
        JobId = jobId == Guid.Empty ? throw new ArgumentException("Job id must not be empty.", nameof(jobId)) : jobId;
        Marker = marker < 0 ? throw new ArgumentOutOfRangeException(nameof(marker), "Checkpoint marker must not be negative.") : marker;
        UpdatedAt = updatedAt;
        Tables = Array.Empty<MigrationTableCheckpoint>();
        Errors = Array.Empty<MigrationExecutionError>();
        ResumeGuidance = "Resume will continue from the first incomplete table or batch. Completed source ids are skipped.";
    }

    public Guid CheckpointId { get; init; }

    public Guid RunId { get; init; }

    public Guid JobId { get; init; }

    public long Marker { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }

    public IReadOnlyList<MigrationTableCheckpoint> Tables { get; init; }

    public IReadOnlyList<MigrationExecutionError> Errors { get; init; }

    public string ResumeGuidance { get; init; }

    public MigrationCheckpoint Advance(
        IReadOnlyList<MigrationTableCheckpoint> tables,
        IReadOnlyList<MigrationExecutionError> errors,
        string resumeGuidance,
        DateTimeOffset now) => this with
    {
        Marker = Marker + 1,
        UpdatedAt = now,
        Tables = tables,
        Errors = errors,
        ResumeGuidance = resumeGuidance
    };
}

public sealed record MigrationTableCheckpoint(
    string TableLogicalName,
    MigrationJobStatus Status,
    MigrationTableIdempotency Idempotency,
    int RecordsRead,
    int RecordsWritten,
    int RecordsSkipped,
    int RecordsFailed,
    int LastCompletedBatchNumber,
    int LastProcessedOffset,
    string? LastProcessedKey,
    string? DeltaToken,
    IReadOnlyList<MigrationBatchCheckpoint> Batches,
    IReadOnlyList<MigrationRecordCheckpoint> Records)
{
    public bool IsCompleted => Status == MigrationJobStatus.Completed;
}

public sealed record MigrationBatchCheckpoint(
    int BatchNumber,
    MigrationCheckpointUnitStatus Status,
    int Attempt,
    int RecordsRead,
    int RecordsWritten,
    int RecordsSkipped,
    int RecordsFailed);

public sealed record MigrationRecordCheckpoint(
    Guid SourceId,
    Guid? TargetId,
    MigrationCheckpointUnitStatus Status,
    int Attempt,
    string? ErrorCode,
    MigrationRecordWriteDisposition Disposition = MigrationRecordWriteDisposition.Unknown);

public enum MigrationCheckpointUnitStatus
{
    Pending,
    Completed,
    RetryPending,
    TerminalFailed,
    Skipped
}
