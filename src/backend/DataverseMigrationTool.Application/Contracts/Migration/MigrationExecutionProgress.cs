using DataverseMigrationTool.Domain.Enums;

namespace DataverseMigrationTool.Application.Contracts.Migration;

public sealed record MigrationExecutionProgress(
    Guid JobId,
    Guid RunId,
    MigrationJobStatus Status,
    string Stage,
    string? TableLogicalName,
    int RecordsRead,
    int RecordsWritten,
    int RecordsSkipped,
    int RecordsFailed);
