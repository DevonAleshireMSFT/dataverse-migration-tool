using DataverseMigrationTool.Application.Ports;
using Microsoft.Extensions.Logging;

namespace DataverseMigrationTool.Infrastructure.Logging;

public sealed class MicrosoftExtensionsOperationLogger(
    ILogger<MicrosoftExtensionsOperationLogger> logger) : IOperationLogger
{
    public Task RecordAsync(
        Guid jobId,
        string operation,
        string message,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Migration operation {Operation} for job {JobId}: {Message}",
            operation,
            jobId,
            message);

        return Task.CompletedTask;
    }
}

