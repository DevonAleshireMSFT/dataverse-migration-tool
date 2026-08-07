using DataverseMigrationTool.Application.Contracts.Metadata;
using DataverseMigrationTool.Application.Ports;
using DataverseMigrationTool.Domain.ValueObjects.Metadata;

namespace DataverseMigrationTool.Infrastructure.Metadata;

/// <summary>
/// Dataverse-backed metadata discovery entry point. The current provider seam establishes a supported Dataverse session;
/// the EntityDefinitions/RetrieveMetadataChanges projection will be filled in here as the provider exposes metadata execution.
/// </summary>
public sealed class SupportedDataverseMetadataDiscoveryService(IDataverseProvider dataverseProvider) : IMetadataDiscoveryService
{
    public async Task<MetadataDiscoveryResult> DiscoverAsync(
        MetadataDiscoveryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await dataverseProvider.ConnectAsync(request.Environment, cancellationToken);

        // TODO(#20): Project supported Dataverse EntityDefinitions/RetrieveMetadataChanges responses into the domain read models.
        MetadataSnapshot snapshot = new(
            request.Environment,
            request.Scope,
            DateTimeOffset.UtcNow,
            Array.Empty<TableMetadata>(),
            Array.Empty<ChoiceMetadata>());

        return new MetadataDiscoveryResult(snapshot, SatisfiedFromCache: false, CompletedAt: DateTimeOffset.UtcNow);
    }
}
