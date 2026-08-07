using System.Collections.Concurrent;
using DataverseMigrationTool.Application.Contracts.Metadata;
using DataverseMigrationTool.Application.Ports;
using DataverseMigrationTool.Domain.ValueObjects.Metadata;

namespace DataverseMigrationTool.Infrastructure.Metadata;

/// <summary>
/// Adds explicit, safe metadata caching around an inner discovery service. Cache keys use environment plus scope.
/// Default TTL is fifteen minutes unless the request supplies <see cref="MetadataCachePolicy.TimeToLive"/>.
/// Use <see cref="MetadataCachePolicy.BypassCache"/> to force a fresh Dataverse read and refresh the cache.
/// </summary>
public sealed class CachingMetadataDiscoveryService(
    IMetadataDiscoveryService inner,
    IMetadataCache cache,
    TimeSpan? defaultTimeToLive = null) : IMetadataDiscoveryService
{
    private static readonly TimeSpan BuiltInDefaultTimeToLive = TimeSpan.FromMinutes(15);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> keyLocks = new(StringComparer.Ordinal);
    private readonly TimeSpan effectiveDefaultTimeToLive = defaultTimeToLive ?? BuiltInDefaultTimeToLive;

    public async Task<MetadataDiscoveryResult> DiscoverAsync(
        MetadataDiscoveryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        MetadataCacheKey key = MetadataCacheKey.From(request.Environment, request.Scope);
        TimeSpan ttl = request.CachePolicy.TimeToLive ?? effectiveDefaultTimeToLive;

        if (!request.CachePolicy.BypassCache)
        {
            MetadataSnapshot? cachedSnapshot = await cache.GetAsync(key, cancellationToken);
            if (cachedSnapshot is not null)
            {
                return new MetadataDiscoveryResult(cachedSnapshot, SatisfiedFromCache: true, DateTimeOffset.UtcNow);
            }
        }

        SemaphoreSlim keyLock = keyLocks.GetOrAdd(key.StableKey, static _ => new SemaphoreSlim(1, 1));
        await keyLock.WaitAsync(cancellationToken);
        try
        {
            if (!request.CachePolicy.BypassCache)
            {
                MetadataSnapshot? cachedSnapshot = await cache.GetAsync(key, cancellationToken);
                if (cachedSnapshot is not null)
                {
                    return new MetadataDiscoveryResult(cachedSnapshot, SatisfiedFromCache: true, DateTimeOffset.UtcNow);
                }
            }

            MetadataDiscoveryResult freshResult = await inner.DiscoverAsync(request, cancellationToken);
            await cache.SetAsync(key, freshResult.Snapshot, ttl, cancellationToken);

            return freshResult with { SatisfiedFromCache = false, CompletedAt = DateTimeOffset.UtcNow };
        }
        finally
        {
            keyLock.Release();
        }
    }
}
