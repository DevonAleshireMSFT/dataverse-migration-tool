using DataverseMigrationTool.Domain.ValueObjects.Metadata;

namespace DataverseMigrationTool.Application.Contracts.Metadata;

/// <summary>
/// Result returned to compare, validation, and UI consumers after metadata discovery.
/// </summary>
public sealed record MetadataDiscoveryResult(
    MetadataSnapshot Snapshot,
    bool SatisfiedFromCache,
    DateTimeOffset CompletedAt);
