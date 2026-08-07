namespace DataverseMigrationTool.Application.Contracts.Metadata;

/// <summary>
/// Explicit cache behavior for metadata discovery. The default allows cache reads and writes with service default TTL.
/// Set <see cref="BypassCache"/> to force a fresh supported Dataverse metadata read while still refreshing the cache.
/// </summary>
public sealed record MetadataCachePolicy(bool BypassCache = false, TimeSpan? TimeToLive = null)
{
    public static MetadataCachePolicy Default { get; } = new();

    public static MetadataCachePolicy Refresh(TimeSpan? timeToLive = null) => new(BypassCache: true, TimeToLive: timeToLive);
}
