using DataverseMigrationTool.Domain.ValueObjects;
using DataverseMigrationTool.Domain.ValueObjects.Metadata;

namespace DataverseMigrationTool.Application.Contracts.Metadata;

/// <summary>
/// Request for a full or scoped Dataverse metadata snapshot.
/// </summary>
public sealed record MetadataDiscoveryRequest(
    EnvironmentProfile Environment,
    MetadataDiscoveryScope Scope,
    MetadataCachePolicy CachePolicy)
{
    public static MetadataDiscoveryRequest ForAllTables(
        EnvironmentProfile environment,
        MetadataCachePolicy? cachePolicy = null) => new(
            environment,
            MetadataDiscoveryScope.All,
            cachePolicy ?? MetadataCachePolicy.Default);
}
