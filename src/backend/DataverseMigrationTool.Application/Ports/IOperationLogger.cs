namespace DataverseMigrationTool.Application.Ports;

public interface IOperationLogger
{
    Task RecordAsync(
        Guid jobId,
        string operation,
        string message,
        CancellationToken cancellationToken = default);
}

