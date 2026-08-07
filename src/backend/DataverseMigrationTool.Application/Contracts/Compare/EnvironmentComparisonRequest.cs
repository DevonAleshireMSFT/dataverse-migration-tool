using DataverseMigrationTool.Application.Contracts.Metadata;
using DataverseMigrationTool.Domain.ValueObjects;
using DataverseMigrationTool.Domain.ValueObjects.Metadata;

namespace DataverseMigrationTool.Application.Contracts.Compare;

public sealed record EnvironmentComparisonRequest(
    EnvironmentProfile SourceEnvironment,
    EnvironmentProfile TargetEnvironment,
    MetadataDiscoveryScope Scope,
    MetadataCachePolicy CachePolicy)
{
    public static EnvironmentComparisonRequest ForAllTables(
        EnvironmentProfile sourceEnvironment,
        EnvironmentProfile targetEnvironment,
        MetadataCachePolicy? cachePolicy = null) => new(
            sourceEnvironment,
            targetEnvironment,
            MetadataDiscoveryScope.All,
            cachePolicy ?? MetadataCachePolicy.Default);
}
