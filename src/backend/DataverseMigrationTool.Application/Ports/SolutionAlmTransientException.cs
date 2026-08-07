namespace DataverseMigrationTool.Application.Ports;

public sealed class SolutionAlmTransientException : Exception
{
    public SolutionAlmTransientException(string message, TimeSpan? retryAfter = null, Exception? innerException = null)
        : base(message, innerException)
    {
        RetryAfter = retryAfter;
    }

    public TimeSpan? RetryAfter { get; }
}
