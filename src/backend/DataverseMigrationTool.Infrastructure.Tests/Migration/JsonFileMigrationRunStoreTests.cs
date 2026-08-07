using DataverseMigrationTool.Application.Contracts.Migration;
using DataverseMigrationTool.Domain.Enums;
using DataverseMigrationTool.Infrastructure.Migration;

namespace DataverseMigrationTool.Infrastructure.Tests.Migration;

public sealed class JsonFileMigrationRunStoreTests
{
    [Fact]
    public async Task SaveAsync_persists_run_state_for_later_store_instance()
    {
        string stateDirectory = Path.Combine(AppContext.BaseDirectory, "migration-store-tests");
        string statePath = Path.Combine(stateDirectory, $"{Guid.NewGuid():N}.json");
        try
        {
            Guid jobId = Guid.NewGuid();
            MigrationRun run = new MigrationRun(Guid.NewGuid(), jobId, MigrationJobStatus.Loading, DateTimeOffset.UtcNow)
                .WithStatus(MigrationJobStatus.Completed, DateTimeOffset.UtcNow);

            await new JsonFileMigrationRunStore(statePath).SaveAsync(run);

            MigrationRun? restored = await new JsonFileMigrationRunStore(statePath).FindLatestForJobAsync(jobId);

            Assert.NotNull(restored);
            Assert.Equal(MigrationJobStatus.Completed, restored.Status);
            Assert.Equal(run.RunId, restored.RunId);
        }
        finally
        {
            if (File.Exists(statePath))
            {
                File.Delete(statePath);
            }
        }
    }

    [Fact]
    public async Task SaveCheckpointAsync_persists_checkpoint_and_updates_run_read_model()
    {
        string stateDirectory = Path.Combine(AppContext.BaseDirectory, "migration-store-tests", $"{Guid.NewGuid():N}");
        string statePath = Path.Combine(stateDirectory, "migration-runs.json");
        try
        {
            Guid jobId = Guid.NewGuid();
            Guid runId = Guid.NewGuid();
            JsonFileMigrationRunStore store = new(statePath);
            await store.SaveAsync(new MigrationRun(runId, jobId, MigrationJobStatus.Running, DateTimeOffset.UtcNow));
            MigrationCheckpoint checkpoint = new MigrationCheckpoint(Guid.NewGuid(), runId, jobId, 0, DateTimeOffset.UtcNow).Advance(
                [
                    new MigrationTableCheckpoint(
                        "account",
                        MigrationJobStatus.Running,
                        new MigrationTableIdempotency(MigrationIdempotencyMode.AlternateKey, ["accountnumber"], "Uses alternate key."),
                        RecordsRead: 2,
                        RecordsWritten: 1,
                        RecordsSkipped: 0,
                        RecordsFailed: 1,
                        LastCompletedBatchNumber: 1,
                        LastProcessedOffset: 2,
                        LastProcessedKey: "source-key",
                        DeltaToken: "delta",
                        Batches:
                        [
                            new MigrationBatchCheckpoint(1, MigrationCheckpointUnitStatus.RetryPending, Attempt: 2, RecordsRead: 2, RecordsWritten: 1, RecordsSkipped: 0, RecordsFailed: 1)
                        ],
                        Records:
                        [
                            new MigrationRecordCheckpoint(Guid.NewGuid(), Guid.NewGuid(), MigrationCheckpointUnitStatus.Completed, Attempt: 1, ErrorCode: null)
                        ])
                ],
                [new MigrationExecutionError("account", null, "Timeout", "Redacted failure.", true, "Resume.", 2)],
                "Resume from account batch 1.",
                DateTimeOffset.UtcNow);

            await store.SaveCheckpointAsync(checkpoint);

            MigrationCheckpoint? roundTripped = await new JsonFileMigrationRunStore(statePath).FindLatestCheckpointForJobAsync(jobId);
            MigrationRun? roundTrippedRun = await new JsonFileMigrationRunStore(statePath).FindAsync(runId);

            Assert.NotNull(roundTripped);
            Assert.Equal(checkpoint.Marker, roundTripped.Marker);
            Assert.Equal("account", roundTripped.Tables.Single().TableLogicalName);
            Assert.Equal(MigrationCheckpointUnitStatus.RetryPending, roundTripped.Tables.Single().Batches.Single().Status);
            Assert.Equal("Resume from account batch 1.", roundTrippedRun!.ResumeGuidance);
            Assert.NotNull(roundTrippedRun.Checkpoint);
        }
        finally
        {
            if (Directory.Exists(stateDirectory))
            {
                Directory.Delete(stateDirectory, recursive: true);
            }
        }
    }
}
