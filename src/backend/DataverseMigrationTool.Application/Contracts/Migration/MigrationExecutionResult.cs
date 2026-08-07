namespace DataverseMigrationTool.Application.Contracts.Migration;

public sealed record MigrationExecutionResult(
    Guid JobId,
    Guid RunId,
    bool Succeeded,
    IReadOnlyList<MigrationTableExecutionSummary> Tables,
    IReadOnlyList<MigrationExecutionError> Errors)
{
    public int RecordsRead => Tables.Sum(table => table.RecordsRead);

    public int RecordsWritten => Tables.Sum(table => table.RecordsWritten);

    public int RecordsSkipped => Tables.Sum(table => table.RecordsSkipped);

    public int RecordsFailed => Tables.Sum(table => table.RecordsFailed);
}

public sealed record MigrationTableExecutionSummary(
    string TableLogicalName,
    int RecordsRead,
    int RecordsWritten,
    int RecordsSkipped,
    int RecordsFailed);

public sealed record MigrationExecutionError(
    string TableLogicalName,
    Guid? SourceRecordId,
    string Code,
    string Message,
    bool Retryable,
    string OperatorAction,
    int Attempt);
