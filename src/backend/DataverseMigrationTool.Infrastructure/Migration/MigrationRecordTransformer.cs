using DataverseMigrationTool.Application.Contracts.Migration;

namespace DataverseMigrationTool.Infrastructure.Migration;

public sealed class MigrationRecordTransformer
{
    public TransformedMigrationRecord Transform(MigrationRecord record, MigrationIdMap idMap)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(idMap);

        Dictionary<string, MigrationTargetLookupValue> immediateLookups = new(StringComparer.OrdinalIgnoreCase);
        List<DeferredRelationshipPatch> deferredPatches = [];

        foreach (MigrationLookupValue lookup in record.Lookups)
        {
            if (idMap.TryGetTargetId(lookup.TargetTableLogicalName, lookup.SourceTargetId, out Guid targetId))
            {
                immediateLookups[lookup.FieldLogicalName] = new MigrationTargetLookupValue(lookup.TargetTableLogicalName, targetId);
            }
            else
            {
                deferredPatches.Add(new DeferredRelationshipPatch(
                    record.TableLogicalName,
                    record.SourceId,
                    lookup.FieldLogicalName,
                    lookup.TargetTableLogicalName,
                    lookup.SourceTargetId));
            }
        }

        return new TransformedMigrationRecord(
            new MigrationRecordWriteRequest(record.TableLogicalName, record.SourceId, record.Attributes, immediateLookups),
            deferredPatches);
    }
}

public sealed record TransformedMigrationRecord(
    MigrationRecordWriteRequest WriteRequest,
    IReadOnlyList<DeferredRelationshipPatch> DeferredPatches);
