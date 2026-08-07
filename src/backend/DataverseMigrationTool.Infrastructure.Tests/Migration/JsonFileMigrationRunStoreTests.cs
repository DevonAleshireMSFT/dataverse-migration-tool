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
}
