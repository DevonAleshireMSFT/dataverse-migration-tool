using System.Collections.Concurrent;
using DataverseMigrationTool.Application.Contracts.Metadata;
using DataverseMigrationTool.Application.Ports;
using DataverseMigrationTool.Domain.ValueObjects;
using DataverseMigrationTool.Domain.ValueObjects.Metadata;

namespace DataverseMigrationTool.Infrastructure.Metadata;

/// <summary>
/// Thread-safe in-memory metadata snapshot cache. Entries are keyed by environment and normalized scope,
/// expire after the supplied TTL, and are removed only by expiry or explicit invalidation.
/// </summary>
public sealed class InMemoryMetadataCache(TimeProvider? timeProvider = null) : IMetadataCache
{
    private readonly ConcurrentDictionary<string, CacheEntry> entries = new(StringComparer.Ordinal);
    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;

    public ValueTask<MetadataSnapshot?> GetAsync(MetadataCacheKey key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        cancellationToken.ThrowIfCancellationRequested();

        if (!entries.TryGetValue(key.StableKey, out CacheEntry? entry))
        {
            return ValueTask.FromResult<MetadataSnapshot?>(null);
        }

        if (entry.ExpiresAt <= clock.GetUtcNow())
        {
            entries.TryRemove(key.StableKey, out _);
            return ValueTask.FromResult<MetadataSnapshot?>(null);
        }

        return ValueTask.FromResult<MetadataSnapshot?>(entry.Snapshot);
    }

    public ValueTask SetAsync(
        MetadataCacheKey key,
        MetadataSnapshot snapshot,
        TimeSpan timeToLive,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(snapshot);
        cancellationToken.ThrowIfCancellationRequested();

        if (timeToLive <= TimeSpan.Zero)
        {
            entries.TryRemove(key.StableKey, out _);
            return ValueTask.CompletedTask;
        }

        entries[key.StableKey] = new CacheEntry(key, snapshot, clock.GetUtcNow().Add(timeToLive));
        return ValueTask.CompletedTask;
    }

    public ValueTask InvalidateAsync(MetadataCacheKey key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        cancellationToken.ThrowIfCancellationRequested();

        entries.TryRemove(key.StableKey, out _);
        return ValueTask.CompletedTask;
    }

    public ValueTask InvalidateEnvironmentAsync(EnvironmentProfile environment, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(environment);
        cancellationToken.ThrowIfCancellationRequested();

        Uri normalizedEnvironmentUrl = new(environment.Url.GetLeftPart(UriPartial.Authority));
        foreach (KeyValuePair<string, CacheEntry> entry in entries)
        {
            MetadataCacheKey key = entry.Value.Key;
            if (key.TenantId == environment.TenantId
                && key.Cloud == environment.Cloud.ToString()
                && key.EnvironmentUrl == normalizedEnvironmentUrl)
            {
                entries.TryRemove(entry.Key, out _);
            }
        }

        return ValueTask.CompletedTask;
    }

    private sealed record CacheEntry(MetadataCacheKey Key, MetadataSnapshot Snapshot, DateTimeOffset ExpiresAt);
}
