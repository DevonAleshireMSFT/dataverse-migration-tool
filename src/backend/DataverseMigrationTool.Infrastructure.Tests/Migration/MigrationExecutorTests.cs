using System.Runtime.CompilerServices;
using DataverseMigrationTool.Application.Contracts.Metadata;
using DataverseMigrationTool.Application.Contracts.Migration;
using DataverseMigrationTool.Application.Ports;
using DataverseMigrationTool.Domain.Entities;
using DataverseMigrationTool.Domain.Enums;
using DataverseMigrationTool.Domain.ValueObjects;
using DataverseMigrationTool.Domain.ValueObjects.Metadata;
using DataverseMigrationTool.Domain.ValueObjects.Validation;
using DataverseMigrationTool.Infrastructure.Jobs;
using DataverseMigrationTool.Infrastructure.Migration;

namespace DataverseMigrationTool.Infrastructure.Tests.Migration;

public sealed class MigrationExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_blocks_on_validation_blockers()
    {
        MigrationJob job = Job("account");
        FakeMigrationDataProvider dataProvider = new();
        InMemoryMigrationRunStore runStore = new();
        MigrationExecutor executor = CreateExecutor(
            dataProvider,
            runStore,
            ValidationReport.FromFindings([new ValidationFinding("blocker", "Fix permissions.", ValidationSeverity.Blocker, "Security", "account")]),
            Snapshot("account"));

        MigrationExecutionResult result = await executor.ExecuteAsync(job);

        Assert.False(result.Succeeded);
        Assert.Empty(dataProvider.ExtractedTables);
        Assert.Equal(MigrationJobStatus.Failed, job.Status);
        Assert.Equal(MigrationJobStatus.Failed, (await runStore.FindLatestForJobAsync(job.Id))!.Status);
    }

    [Fact]
    public async Task ExecuteAsync_batches_loads_and_retries_retryable_record_once()
    {
        MigrationJob job = Job("account");
        Guid sourceId = Guid.NewGuid();
        Guid targetId = Guid.NewGuid();
        FakeMigrationDataProvider dataProvider = new()
        {
            RecordsByTable =
            {
                ["account"] =
                [
                    new MigrationRecord("account", sourceId, new Dictionary<string, object?>(), Array.Empty<MigrationLookupValue>(), Array.Empty<MigrationManyToManyLink>())
                ]
            },
            UpsertHandler = (records, call) => call == 1
                ? [new MigrationRecordWriteResult("account", sourceId, null, false, new MigrationExecutionError("account", sourceId, "Timeout", "Transient failure.", true, "Retry.", 0))]
                : [new MigrationRecordWriteResult("account", sourceId, targetId, true, null)]
        };
        MigrationExecutor executor = CreateExecutor(dataProvider, new InMemoryMigrationRunStore(), ValidationReport.Empty, Snapshot("account"));

        MigrationExecutionResult result = await executor.ExecuteAsync(job, new MigrationExecutionOptions(new MigrationBatchSettings(maxBatchSize: 1, maxRetryAttempts: 1)));

        Assert.True(result.Succeeded);
        Assert.Equal(2, dataProvider.UpsertCallCount);
        Assert.Equal(1, result.RecordsWritten);
        Assert.Empty(result.Errors);
        Assert.Equal(MigrationJobStatus.Completed, job.Status);
    }

    [Fact]
    public async Task ExecuteAsync_defers_and_patches_self_relationships_after_id_mapping_exists()
    {
        MigrationJob job = Job("account");
        Guid parentSource = Guid.NewGuid();
        Guid childSource = Guid.NewGuid();
        Guid parentTarget = Guid.NewGuid();
        Guid childTarget = Guid.NewGuid();
        FakeMigrationDataProvider dataProvider = new()
        {
            RecordsByTable =
            {
                ["account"] =
                [
                    new MigrationRecord("account", parentSource, new Dictionary<string, object?>(), Array.Empty<MigrationLookupValue>(), Array.Empty<MigrationManyToManyLink>()),
                    new MigrationRecord("account", childSource, new Dictionary<string, object?>(), [new MigrationLookupValue("parentaccountid", "account", parentSource)], Array.Empty<MigrationManyToManyLink>())
                ]
            },
            TargetIds = { [parentSource] = parentTarget, [childSource] = childTarget }
        };
        MetadataSnapshot snapshot = Snapshot("account", [new RelationshipMetadata("account_parent", MetadataRelationshipType.ManyToOne, "account", "parentaccountid", "account", null, null, true)]);
        MigrationExecutor executor = CreateExecutor(dataProvider, new InMemoryMigrationRunStore(), ValidationReport.Empty, snapshot);

        MigrationExecutionResult result = await executor.ExecuteAsync(job, new MigrationExecutionOptions(new MigrationBatchSettings(maxBatchSize: 2, maxRetryAttempts: 0)));

        Assert.True(result.Succeeded);
        MigrationRelationshipPatchRequest patch = Assert.Single(dataProvider.Patches);
        Assert.Equal(childTarget, patch.TargetId);
        Assert.Equal(parentTarget, patch.Lookup.TargetId);
    }

    private static MigrationExecutor CreateExecutor(
        FakeMigrationDataProvider dataProvider,
        IMigrationRunStore runStore,
        ValidationReport validationReport,
        MetadataSnapshot metadata)
    {
        return new MigrationExecutor(
            new FakeValidationEngine(validationReport),
            new FakeMetadataDiscoveryService(metadata),
            dataProvider,
            runStore,
            new InMemoryMigrationJobStore(),
            new FakeOperationLogger(),
            new MigrationExecutionPlanner(),
            new MigrationRecordTransformer());
    }

    private static MigrationJob Job(params string[] tables) => new(
        Guid.NewGuid(),
        new EnvironmentProfile("source", new Uri("https://source.example.crm.dynamics.com"), Guid.NewGuid(), DataverseCloud.Public),
        new EnvironmentProfile("target", new Uri("https://target.example.crm.dynamics.com"), Guid.NewGuid(), DataverseCloud.Public),
        new ComponentSelection(true, false, tables, Array.Empty<string>()),
        MigrationMode.Full);

    private static MetadataSnapshot Snapshot(string table, IReadOnlyList<RelationshipMetadata>? relationships = null) => new(
        new EnvironmentProfile("source", new Uri("https://source.example.crm.dynamics.com"), Guid.NewGuid(), DataverseCloud.Public),
        new MetadataDiscoveryScope([table]),
        DateTimeOffset.UtcNow,
        [new TableMetadata(table, table, table, null, true, false, false, Array.Empty<FieldMetadata>(), relationships ?? Array.Empty<RelationshipMetadata>(), Array.Empty<AlternateKeyMetadata>())],
        Array.Empty<ChoiceMetadata>());

    private sealed class FakeValidationEngine(ValidationReport report) : IValidationEngine
    {
        public Task<ValidationReport> ValidateAsync(MigrationJob job, CancellationToken cancellationToken = default) => Task.FromResult(report);
    }

    private sealed class FakeMetadataDiscoveryService(MetadataSnapshot metadata) : IMetadataDiscoveryService
    {
        public Task<MetadataDiscoveryResult> DiscoverAsync(MetadataDiscoveryRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new MetadataDiscoveryResult(metadata, false, DateTimeOffset.UtcNow));
    }

    private sealed class FakeOperationLogger : IOperationLogger
    {
        public Task RecordAsync(Guid jobId, string operation, string message, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeMigrationDataProvider : IMigrationDataProvider
    {
        public Dictionary<string, IReadOnlyList<MigrationRecord>> RecordsByTable { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<Guid, Guid> TargetIds { get; } = new();

        public List<string> ExtractedTables { get; } = [];

        public List<MigrationRelationshipPatchRequest> Patches { get; } = [];

        public int UpsertCallCount { get; private set; }

        public Func<IReadOnlyList<MigrationRecordWriteRequest>, int, IReadOnlyList<MigrationRecordWriteResult>>? UpsertHandler { get; set; }

        public async IAsyncEnumerable<MigrationRecord> ExtractRecordsAsync(MigrationDataReadRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            ExtractedTables.Add(request.TableLogicalName);
            foreach (MigrationRecord record in RecordsByTable.GetValueOrDefault(request.TableLogicalName) ?? Array.Empty<MigrationRecord>())
            {
                yield return record;
            }

            await Task.CompletedTask;
        }

        public Task<IReadOnlyList<MigrationRecordWriteResult>> UpsertBatchAsync(EnvironmentProfile target, IReadOnlyList<MigrationRecordWriteRequest> records, CancellationToken cancellationToken = default)
        {
            UpsertCallCount++;
            if (UpsertHandler is not null)
            {
                return Task.FromResult(UpsertHandler(records, UpsertCallCount));
            }

            IReadOnlyList<MigrationRecordWriteResult> results = records
                .Select(record => new MigrationRecordWriteResult(record.TableLogicalName, record.SourceId, TargetIds.GetValueOrDefault(record.SourceId, record.SourceId), true, null))
                .ToArray();
            return Task.FromResult(results);
        }

        public Task<IReadOnlyList<MigrationExecutionError>> PatchRelationshipsAsync(EnvironmentProfile target, IReadOnlyList<MigrationRelationshipPatchRequest> patches, CancellationToken cancellationToken = default)
        {
            Patches.AddRange(patches);
            return Task.FromResult<IReadOnlyList<MigrationExecutionError>>(Array.Empty<MigrationExecutionError>());
        }
    }
}
