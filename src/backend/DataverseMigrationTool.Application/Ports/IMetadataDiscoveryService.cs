using DataverseMigrationTool.Application.Contracts.Metadata;

namespace DataverseMigrationTool.Application.Ports;

/// <summary>
/// Discovers Dataverse table, field, relationship, key, and choice metadata using supported APIs only.
/// Implementations must be cancellable and must not expose bearer tokens or SDK-specific types.
/// </summary>
public interface IMetadataDiscoveryService
{
    Task<MetadataDiscoveryResult> DiscoverAsync(
        MetadataDiscoveryRequest request,
        CancellationToken cancellationToken = default);
}
