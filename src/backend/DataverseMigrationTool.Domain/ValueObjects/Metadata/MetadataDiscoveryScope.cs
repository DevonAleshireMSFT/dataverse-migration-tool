namespace DataverseMigrationTool.Domain.ValueObjects.Metadata;

/// <summary>
/// A stable metadata discovery scope. Empty table names mean all tables.
/// </summary>
/// <param name="TableLogicalNames">Logical table names requested for a scoped discovery.</param>
public sealed record MetadataDiscoveryScope(IReadOnlyCollection<string> TableLogicalNames)
{
    public static MetadataDiscoveryScope All { get; } = new(Array.Empty<string>());

    public bool IsAllTables => TableLogicalNames.Count == 0;
}
