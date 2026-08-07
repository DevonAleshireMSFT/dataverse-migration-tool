using DataverseMigrationTool.Domain.ValueObjects.Compare;
using DataverseMigrationTool.Domain.ValueObjects.Metadata;

namespace DataverseMigrationTool.Application.Ports;

public interface IMetadataSnapshotComparer
{
    EnvironmentComparisonReport Compare(MetadataSnapshot sourceSnapshot, MetadataSnapshot targetSnapshot);
}
