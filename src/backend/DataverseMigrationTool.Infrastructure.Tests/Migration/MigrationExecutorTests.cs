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
                : [new MigrationRecordWriteResult("account", sourceId, targetId, true, null, MigrationRecordWriteDisposition.Created)]
        };
        InMemoryMigrationRunStore runStore = new();
        MigrationExecutor executor = CreateExecutor(dataProvider, runStore, ValidationReport.Empty, Snapshot("account"));

        MigrationExecutionResult result = await executor.ExecuteAsync(job, new MigrationExecutionOptions(new MigrationBatchSettings(maxBatchSize: 1, maxRetryAttempts: 1)));

        Assert.True(result.Succeeded);
        Assert.Equal(2, dataProvider.UpsertCallCount);
        Assert.Equal(1, result.RecordsWritten);
        Assert.Empty(result.Errors);
        Assert.Equal(MigrationJobStatus.Completed, job.Status);
        RollbackGuidance guidance = (await runStore.FindLatestRollbackGuidanceForJobAsync(job.Id))!;
        Assert.Equal(RollbackReversibility.ReversibleViaSupportedApi, Assert.Single(guidance.Actions).Reversibility);
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

    [Fact]
    public async Task ResumeAsync_skips_completed_table_from_checkpoint()
    {
        MigrationJob job = Job("account");
        Guid sourceId = Guid.NewGuid();
        InMemoryMigrationRunStore runStore = new();
        FakeMigrationDataProvider firstProvider = new()
        {
            RecordsByTable =
            {
                ["account"] =
                [
                    new MigrationRecord("account", sourceId, new Dictionary<string, object?>(), Array.Empty<MigrationLookupValue>(), Array.Empty<MigrationManyToManyLink>())
                ]
            }
        };
        await CreateExecutor(firstProvider, runStore, ValidationReport.Empty, Snapshot("account"))
            .ExecuteAsync(job, new MigrationExecutionOptions(new MigrationBatchSettings(maxBatchSize: 1, maxRetryAttempts: 0)));
        FakeMigrationDataProvider resumeProvider = new();

        MigrationExecutionResult result = await CreateExecutor(resumeProvider, runStore, ValidationReport.Empty, Snapshot("account")).ResumeAsync(job);

        Assert.True(result.Succeeded);
        Assert.Empty(resumeProvider.ExtractedTables);
        Assert.Equal(1, result.RecordsWritten);
    }

    [Fact]
    public async Task ExecuteAsync_rerun_with_alternate_key_does_not_create_duplicate()
    {
        MigrationJob job = Job("account");
        Guid sourceId = Guid.NewGuid();
        Dictionary<string, object?> attributes = new(StringComparer.OrdinalIgnoreCase) { ["accountnumber"] = "A-100" };
        FakeMigrationDataProvider dataProvider = new()
        {
            RecordsByTable =
            {
                ["account"] =
                [
                    new MigrationRecord("account", sourceId, attributes, Array.Empty<MigrationLookupValue>(), Array.Empty<MigrationManyToManyLink>())
                ]
            },
            UseAlternateKeyStore = true
        };
        MetadataSnapshot metadata = Snapshot("account", alternateKeys: [new AlternateKeyMetadata("ak_accountnumber", "ak_accountnumber", "Account Number", ["accountnumber"], IsManaged: false)]);

        await CreateExecutor(dataProvider, new InMemoryMigrationRunStore(), ValidationReport.Empty, metadata).ExecuteAsync(job);
        await CreateExecutor(dataProvider, new InMemoryMigrationRunStore(), ValidationReport.Empty, metadata).ExecuteAsync(job);

        Assert.Equal(1, dataProvider.CreatedTargetCount);
        Assert.All(dataProvider.Requests, request => Assert.Equal(MigrationIdempotencyMode.AlternateKey, request.Idempotency.Mode));
    }

    [Fact]
    public async Task ExecuteAsync_caps_retry_attempts_and_surfaces_terminal_failure()
    {
        MigrationJob job = Job("account");
        Guid sourceId = Guid.NewGuid();
        FakeMigrationDataProvider dataProvider = new()
        {
            RecordsByTable =
            {
                ["account"] =
                [
                    new MigrationRecord("account", sourceId, new Dictionary<string, object?>(), Array.Empty<MigrationLookupValue>(), Array.Empty<MigrationManyToManyLink>())
                ]
            },
            UpsertHandler = (records, _) =>
                [new MigrationRecordWriteResult("account", sourceId, null, false, new MigrationExecutionError("account", sourceId, "Timeout", "Transient failure.", true, "Retry.", 0))]
        };
        InMemoryMigrationRunStore runStore = new();

        MigrationExecutionResult result = await CreateExecutor(dataProvider, runStore, ValidationReport.Empty, Snapshot("account"))
            .ExecuteAsync(job, new MigrationExecutionOptions(new MigrationBatchSettings(maxBatchSize: 1, maxRetryAttempts: 1)));

        Assert.False(result.Succeeded);
        Assert.Equal(2, dataProvider.UpsertCallCount);
        MigrationExecutionError error = Assert.Single(result.Errors);
        Assert.Equal(2, error.Attempt);
        Assert.Contains("exhausted", error.OperatorAction, StringComparison.OrdinalIgnoreCase);
        MigrationCheckpoint checkpoint = (await runStore.FindLatestCheckpointForJobAsync(job.Id))!;
        Assert.Equal(MigrationCheckpointUnitStatus.TerminalFailed, checkpoint.Tables.Single().Records.Single().Status);
    }

    [Fact]
    public async Task ExecuteAsync_does_not_retry_terminal_errors()
    {
        MigrationJob job = Job("account");
        Guid sourceId = Guid.NewGuid();
        FakeMigrationDataProvider dataProvider = new()
        {
            RecordsByTable =
            {
                ["account"] =
                [
                    new MigrationRecord("account", sourceId, new Dictionary<string, object?>(), Array.Empty<MigrationLookupValue>(), Array.Empty<MigrationManyToManyLink>())
                ]
            },
            UpsertHandler = (records, _) =>
                [new MigrationRecordWriteResult("account", sourceId, null, false, new MigrationExecutionError("account", sourceId, "Validation", "Terminal failure.", false, "Fix data.", 0))]
        };

        MigrationExecutionResult result = await CreateExecutor(dataProvider, new InMemoryMigrationRunStore(), ValidationReport.Empty, Snapshot("account"))
            .ExecuteAsync(job, new MigrationExecutionOptions(new MigrationBatchSettings(maxBatchSize: 1, maxRetryAttempts: 3)));

        Assert.False(result.Succeeded);
        Assert.Equal(1, dataProvider.UpsertCallCount);
        Assert.False(Assert.Single(result.Errors).Retryable);
    }

    [Fact]
    public async Task ResumeAsync_after_interruption_replays_at_most_uncheckpointed_batch()
    {
        MigrationJob job = Job("account");
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        InMemoryMigrationRunStore runStore = new();
        FakeMigrationDataProvider interruptedProvider = new()
        {
            RecordsByTable =
            {
                ["account"] =
                [
                    new MigrationRecord("account", first, new Dictionary<string, object?>(), Array.Empty<MigrationLookupValue>(), Array.Empty<MigrationManyToManyLink>()),
                    new MigrationRecord("account", second, new Dictionary<string, object?>(), Array.Empty<MigrationLookupValue>(), Array.Empty<MigrationManyToManyLink>())
                ]
            },
            ThrowOnUpsertCall = 2
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateExecutor(interruptedProvider, runStore, ValidationReport.Empty, Snapshot("account"))
            .ExecuteAsync(job, new MigrationExecutionOptions(new MigrationBatchSettings(maxBatchSize: 1, maxRetryAttempts: 0))));
        MigrationCheckpoint checkpoint = (await runStore.FindLatestCheckpointForJobAsync(job.Id))!;
        Assert.Equal(first, checkpoint.Tables.Single().Records.Single().SourceId);

        FakeMigrationDataProvider resumeProvider = new()
        {
            RecordsByTable =
            {
                ["account"] =
                [
                    new MigrationRecord("account", first, new Dictionary<string, object?>(), Array.Empty<MigrationLookupValue>(), Array.Empty<MigrationManyToManyLink>()),
                    new MigrationRecord("account", second, new Dictionary<string, object?>(), Array.Empty<MigrationLookupValue>(), Array.Empty<MigrationManyToManyLink>())
                ]
            }
        };

        MigrationExecutionResult result = await CreateExecutor(resumeProvider, runStore, ValidationReport.Empty, Snapshot("account"))
            .ResumeAsync(job, new MigrationExecutionOptions(new MigrationBatchSettings(maxBatchSize: 1, maxRetryAttempts: 0)));

        Assert.True(result.Succeeded);
        Assert.Single(resumeProvider.Requests);
        Assert.Equal(second, resumeProvider.Requests.Single().SourceId);
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
            new RollbackGuidanceGenerator(),
            new MigrationExecutionPlanner(),
            new MigrationRecordTransformer());
    }

    private static MigrationJob Job(params string[] tables) => new(
        Guid.NewGuid(),
        new EnvironmentProfile("source", new Uri("https://source.example.crm.dynamics.com"), Guid.NewGuid(), DataverseCloud.Public),
        new EnvironmentProfile("target", new Uri("https://target.example.crm.dynamics.com"), Guid.NewGuid(), DataverseCloud.Public),
        new ComponentSelection(true, false, tables, Array.Empty<string>()),
        MigrationMode.Full);

    private static MetadataSnapshot Snapshot(
        string table,
        IReadOnlyList<RelationshipMetadata>? relationships = null,
        IReadOnlyList<AlternateKeyMetadata>? alternateKeys = null) => new(
        new EnvironmentProfile("source", new Uri("https://source.example.crm.dynamics.com"), Guid.NewGuid(), DataverseCloud.Public),
        new MetadataDiscoveryScope([table]),
        DateTimeOffset.UtcNow,
        [new TableMetadata(table, table, table, null, true, false, false, Array.Empty<FieldMetadata>(), relationships ?? Array.Empty<RelationshipMetadata>(), alternateKeys ?? Array.Empty<AlternateKeyMetadata>())],
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

        public List<MigrationRecordWriteRequest> Requests { get; } = [];

        public bool UseAlternateKeyStore { get; set; }

        public int CreatedTargetCount { get; private set; }

        public int? ThrowOnUpsertCall { get; set; }

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
            if (ThrowOnUpsertCall == UpsertCallCount)
            {
                throw new InvalidOperationException("Simulated interruption.");
            }

            Requests.AddRange(records);
            if (UpsertHandler is not null)
            {
                return Task.FromResult(UpsertHandler(records, UpsertCallCount));
            }

            if (UseAlternateKeyStore)
            {
                IReadOnlyList<MigrationRecordWriteResult> keyedResults = records
                    .Select(record =>
                    {
                        string key = string.Join("|", record.Idempotency.KeyValues.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase).Select(pair => $"{pair.Key}:{pair.Value}"));
                        bool created = !TargetIds.TryGetValue(GuidFromKey(key), out Guid targetId);
                        if (created)
                        {
                            targetId = Guid.NewGuid();
                            TargetIds[GuidFromKey(key)] = targetId;
                            CreatedTargetCount++;
                        }

                        return new MigrationRecordWriteResult(
                            record.TableLogicalName,
                            record.SourceId,
                            targetId,
                            true,
                            null,
                            created ? MigrationRecordWriteDisposition.Created : MigrationRecordWriteDisposition.Updated);
                    })
                    .ToArray();
                return Task.FromResult(keyedResults);
            }

            IReadOnlyList<MigrationRecordWriteResult> results = records
                .Select(record => new MigrationRecordWriteResult(record.TableLogicalName, record.SourceId, TargetIds.GetValueOrDefault(record.SourceId, record.SourceId), true, null, MigrationRecordWriteDisposition.Created))
                .ToArray();
            return Task.FromResult(results);
        }

        public Task<IReadOnlyList<MigrationExecutionError>> PatchRelationshipsAsync(EnvironmentProfile target, IReadOnlyList<MigrationRelationshipPatchRequest> patches, CancellationToken cancellationToken = default)
        {
            Patches.AddRange(patches);
            return Task.FromResult<IReadOnlyList<MigrationExecutionError>>(Array.Empty<MigrationExecutionError>());
        }

        private static Guid GuidFromKey(string key)
        {
            byte[] bytes = new byte[16];
            byte[] source = System.Text.Encoding.UTF8.GetBytes(key);
            Array.Copy(source, bytes, Math.Min(bytes.Length, source.Length));
            return new Guid(bytes);
        }
    }
}
