using DataverseMigrationTool.Application.Contracts.Migration;
using DataverseMigrationTool.Domain.ValueObjects;

namespace DataverseMigrationTool.Application.Ports;

public interface IMigrationDataProvider
{
    IAsyncEnumerable<MigrationRecord> ExtractRecordsAsync(
        MigrationDataReadRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MigrationRecordWriteResult>> UpsertBatchAsync(
        EnvironmentProfile target,
        IReadOnlyList<MigrationRecordWriteRequest> records,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MigrationExecutionError>> PatchRelationshipsAsync(
        EnvironmentProfile target,
        IReadOnlyList<MigrationRelationshipPatchRequest> patches,
        CancellationToken cancellationToken = default);
}
