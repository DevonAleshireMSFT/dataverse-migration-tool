namespace DataverseMigrationTool.Application.Contracts.Migration;

public sealed record MigrationBatchSettings
{
    public MigrationBatchSettings(int maxBatchSize = 100, int maxRetryAttempts = 1)
    {
        if (maxBatchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxBatchSize), "Batch size must be greater than zero.");
        }

        if (maxRetryAttempts < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRetryAttempts), "Retry attempts must not be negative.");
        }

        MaxBatchSize = maxBatchSize;
        MaxRetryAttempts = maxRetryAttempts;
    }

    public int MaxBatchSize { get; }

    public int MaxRetryAttempts { get; }
}
