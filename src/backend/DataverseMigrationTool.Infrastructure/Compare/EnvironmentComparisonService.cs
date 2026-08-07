using DataverseMigrationTool.Application.Contracts.Compare;
using DataverseMigrationTool.Application.Contracts.Metadata;
using DataverseMigrationTool.Application.Ports;
using DataverseMigrationTool.Domain.ValueObjects.Compare;

namespace DataverseMigrationTool.Infrastructure.Compare;

public sealed class EnvironmentComparisonService(
    IMetadataDiscoveryService metadataDiscoveryService,
    IMetadataSnapshotComparer metadataSnapshotComparer) : IEnvironmentComparisonService
{
    public async Task<EnvironmentComparisonReport> CompareAsync(
        EnvironmentComparisonRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        MetadataDiscoveryRequest sourceRequest = new(
            request.SourceEnvironment,
            request.Scope,
            request.CachePolicy);
        MetadataDiscoveryRequest targetRequest = new(
            request.TargetEnvironment,
            request.Scope,
            request.CachePolicy);

        MetadataDiscoveryResult source = await metadataDiscoveryService.DiscoverAsync(sourceRequest, cancellationToken);
        MetadataDiscoveryResult target = await metadataDiscoveryService.DiscoverAsync(targetRequest, cancellationToken);

        return metadataSnapshotComparer.Compare(source.Snapshot, target.Snapshot);
    }
}
