namespace DataverseMigrationTool.Domain.ValueObjects.Metadata;

/// <summary>
/// A complete point-in-time metadata snapshot for an environment and discovery scope.
/// </summary>
public sealed record MetadataSnapshot(
    EnvironmentProfile Environment,
    MetadataDiscoveryScope Scope,
    DateTimeOffset DiscoveredAt,
    IReadOnlyList<TableMetadata> Tables,
    IReadOnlyList<ChoiceMetadata> Choices);
