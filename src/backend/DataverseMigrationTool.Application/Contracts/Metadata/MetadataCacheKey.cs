using DataverseMigrationTool.Domain.ValueObjects;
using DataverseMigrationTool.Domain.ValueObjects.Metadata;

namespace DataverseMigrationTool.Application.Contracts.Metadata;

/// <summary>
/// Stable cache key for metadata snapshots. Keys are derived from the environment URL/cloud/tenant and normalized scope.
/// </summary>
public sealed record MetadataCacheKey(
    string EnvironmentName,
    Uri EnvironmentUrl,
    Guid TenantId,
    string Cloud,
    IReadOnlyCollection<string> TableLogicalNames)
{
    public static MetadataCacheKey From(EnvironmentProfile environment, MetadataDiscoveryScope scope)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(scope);

        string[] tableNames = scope.TableLogicalNames
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .Select(static name => name.Trim().ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        return new MetadataCacheKey(
            environment.Name,
            new Uri(environment.Url.GetLeftPart(UriPartial.Authority)),
            environment.TenantId,
            environment.Cloud.ToString(),
            tableNames);
    }

    public string StableKey => string.Join(
        '|',
        Cloud,
        TenantId.ToString("D"),
        EnvironmentUrl.GetLeftPart(UriPartial.Authority).ToLowerInvariant(),
        string.Join(',', TableLogicalNames));
}
