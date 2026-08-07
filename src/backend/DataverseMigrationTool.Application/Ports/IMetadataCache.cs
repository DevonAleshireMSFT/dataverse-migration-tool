using DataverseMigrationTool.Application.Contracts.Metadata;
using DataverseMigrationTool.Domain.ValueObjects;
using DataverseMigrationTool.Domain.ValueObjects.Metadata;

namespace DataverseMigrationTool.Application.Ports;

/// <summary>
/// Thread-safe metadata cache seam. Entries are keyed by environment and normalized scope and expire by explicit TTL.
/// Invalidation is explicit: callers may invalidate a single key or all entries for an environment after configuration,
/// solution import, or known schema mutation. Implementations must treat cache entries as snapshots and never mutate them.
/// </summary>
public interface IMetadataCache
{
    ValueTask<MetadataSnapshot?> GetAsync(MetadataCacheKey key, CancellationToken cancellationToken = default);

    ValueTask SetAsync(
        MetadataCacheKey key,
        MetadataSnapshot snapshot,
        TimeSpan timeToLive,
        CancellationToken cancellationToken = default);

    ValueTask InvalidateAsync(MetadataCacheKey key, CancellationToken cancellationToken = default);

    ValueTask InvalidateEnvironmentAsync(EnvironmentProfile environment, CancellationToken cancellationToken = default);
}
