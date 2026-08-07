using DataverseMigrationTool.Application.Contracts.Migration;

namespace DataverseMigrationTool.Application.Ports;

public interface IRollbackGuidanceGenerator
{
    RollbackGuidance Generate(MigrationRun run, MigrationCheckpoint checkpoint, DateTimeOffset generatedAt);
}
