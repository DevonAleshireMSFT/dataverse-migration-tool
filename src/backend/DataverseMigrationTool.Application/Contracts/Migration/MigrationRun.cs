using DataverseMigrationTool.Domain.Enums;

namespace DataverseMigrationTool.Application.Contracts.Migration;

public sealed record MigrationRun
{
    public MigrationRun(Guid runId, Guid jobId, MigrationJobStatus status, DateTimeOffset startedAt)
    {
        RunId = runId == Guid.Empty ? throw new ArgumentException("Run id must not be empty.", nameof(runId)) : runId;
        JobId = jobId == Guid.Empty ? throw new ArgumentException("Job id must not be empty.", nameof(jobId)) : jobId;
        Status = status;
        StartedAt = startedAt;
        UpdatedAt = startedAt;
        Tables = Array.Empty<MigrationRunTableState>();
        Errors = Array.Empty<MigrationExecutionError>();
    }

    public Guid RunId { get; init; }

    public Guid JobId { get; init; }

    public MigrationJobStatus Status { get; init; }

    public DateTimeOffset StartedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }

    public DateTimeOffset? CompletedAt { get; init; }

    public IReadOnlyList<MigrationRunTableState> Tables { get; init; }

    public IReadOnlyList<MigrationExecutionError> Errors { get; init; }

    public MigrationRun WithStatus(MigrationJobStatus status, DateTimeOffset now) => this with
    {
        Status = status,
        UpdatedAt = now,
        CompletedAt = status is MigrationJobStatus.Completed or MigrationJobStatus.Failed or MigrationJobStatus.Cancelled ? now : CompletedAt
    };
}

public sealed record MigrationRunTableState(
    string TableLogicalName,
    MigrationJobStatus Status,
    int RecordsRead,
    int RecordsWritten,
    int RecordsSkipped,
    int RecordsFailed,
    int LastCompletedBatchNumber);
