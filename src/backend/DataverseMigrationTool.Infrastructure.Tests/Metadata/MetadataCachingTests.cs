using DataverseMigrationTool.Application.Contracts.Metadata;
using DataverseMigrationTool.Application.Ports;
using DataverseMigrationTool.Domain.Enums;
using DataverseMigrationTool.Domain.ValueObjects;
using DataverseMigrationTool.Domain.ValueObjects.Metadata;
using DataverseMigrationTool.Infrastructure.Metadata;

namespace DataverseMigrationTool.Infrastructure.Tests.Metadata;

public sealed class MetadataCachingTests
{
    [Fact]
    public async Task DiscoverAsync_CachesSnapshotForSameEnvironmentAndScope()
    {
        CountingMetadataDiscoveryService inner = new();
        InMemoryMetadataCache cache = new();
        CachingMetadataDiscoveryService service = new(inner, cache, TimeSpan.FromMinutes(5));
        MetadataDiscoveryRequest request = MetadataDiscoveryRequest.ForAllTables(CreateEnvironment());

        MetadataDiscoveryResult first = await service.DiscoverAsync(request);
        MetadataDiscoveryResult second = await service.DiscoverAsync(request);

        Assert.False(first.SatisfiedFromCache);
        Assert.True(second.SatisfiedFromCache);
        Assert.Equal(1, inner.CallCount);
        Assert.Equal(first.Snapshot.DiscoveredAt, second.Snapshot.DiscoveredAt);
    }

    [Fact]
    public async Task DiscoverAsync_BypassCacheRefreshesSnapshot()
    {
        CountingMetadataDiscoveryService inner = new();
        InMemoryMetadataCache cache = new();
        CachingMetadataDiscoveryService service = new(inner, cache, TimeSpan.FromMinutes(5));
        EnvironmentProfile environment = CreateEnvironment();

        await service.DiscoverAsync(MetadataDiscoveryRequest.ForAllTables(environment));
        MetadataDiscoveryResult refreshed = await service.DiscoverAsync(
            MetadataDiscoveryRequest.ForAllTables(environment, MetadataCachePolicy.Refresh()));

        Assert.False(refreshed.SatisfiedFromCache);
        Assert.Equal(2, inner.CallCount);
    }

    [Fact]
    public async Task InvalidateAsync_RemovesOnlyTheRequestedScope()
    {
        EnvironmentProfile environment = CreateEnvironment();
        InMemoryMetadataCache cache = new();
        MetadataDiscoveryScope allTables = MetadataDiscoveryScope.All;
        MetadataDiscoveryScope accountOnly = new(["account"]);
        MetadataCacheKey allKey = MetadataCacheKey.From(environment, allTables);
        MetadataCacheKey accountKey = MetadataCacheKey.From(environment, accountOnly);

        await cache.SetAsync(allKey, CreateSnapshot(environment, allTables, 1), TimeSpan.FromMinutes(5));
        await cache.SetAsync(accountKey, CreateSnapshot(environment, accountOnly, 2), TimeSpan.FromMinutes(5));

        await cache.InvalidateAsync(accountKey);

        Assert.NotNull(await cache.GetAsync(allKey));
        Assert.Null(await cache.GetAsync(accountKey));
    }

    [Fact]
    public async Task GetAsync_ExpiresEntriesAfterTtl()
    {
        ManualTimeProvider clock = new(DateTimeOffset.Parse("2026-08-06T00:00:00+00:00"));
        InMemoryMetadataCache cache = new(clock);
        EnvironmentProfile environment = CreateEnvironment();
        MetadataDiscoveryScope scope = MetadataDiscoveryScope.All;
        MetadataCacheKey key = MetadataCacheKey.From(environment, scope);

        await cache.SetAsync(key, CreateSnapshot(environment, scope, 1), TimeSpan.FromMinutes(1));
        clock.Advance(TimeSpan.FromMinutes(2));

        Assert.Null(await cache.GetAsync(key));
    }

    private static EnvironmentProfile CreateEnvironment() => new(
        "DEV",
        new Uri("https://org.crm.dynamics.com"),
        Guid.Parse("11111111-1111-1111-1111-111111111111"),
        DataverseCloud.Public);

    private static MetadataSnapshot CreateSnapshot(
        EnvironmentProfile environment,
        MetadataDiscoveryScope scope,
        int sequence) => new(
            environment,
            scope,
            DateTimeOffset.Parse("2026-08-06T00:00:00+00:00").AddMinutes(sequence),
            [new TableMetadata(
                $"account{sequence}",
                $"Account{sequence}",
                $"Account {sequence}",
                Description: null,
                IsCustomTable: false,
                IsActivity: false,
                IsIntersect: false,
                Array.Empty<FieldMetadata>(),
                Array.Empty<RelationshipMetadata>(),
                Array.Empty<AlternateKeyMetadata>())],
            Array.Empty<ChoiceMetadata>());

    private sealed class CountingMetadataDiscoveryService : IMetadataDiscoveryService
    {
        public int CallCount { get; private set; }

        public Task<MetadataDiscoveryResult> DiscoverAsync(
            MetadataDiscoveryRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            MetadataSnapshot snapshot = CreateSnapshot(request.Environment, request.Scope, CallCount);
            return Task.FromResult(new MetadataDiscoveryResult(snapshot, SatisfiedFromCache: false, DateTimeOffset.UtcNow));
        }
    }

    private sealed class ManualTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => utcNow;

        public void Advance(TimeSpan interval) => utcNow = utcNow.Add(interval);
    }
}
