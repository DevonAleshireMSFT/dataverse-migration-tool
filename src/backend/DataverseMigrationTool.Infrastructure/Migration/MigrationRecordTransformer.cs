using DataverseMigrationTool.Application.Contracts.Migration;

namespace DataverseMigrationTool.Infrastructure.Migration;

public sealed class MigrationRecordTransformer
{
    public TransformedMigrationRecord Transform(MigrationRecord record, MigrationIdMap idMap)
        => Transform(record, idMap, new MigrationTableIdempotency(MigrationIdempotencyMode.SourceRecordId, Array.Empty<string>(), "Uses source record ids for idempotent writes."));

    public TransformedMigrationRecord Transform(MigrationRecord record, MigrationIdMap idMap, MigrationTableIdempotency idempotency)
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

        MigrationWriteIdempotency writeIdempotency = CreateWriteIdempotency(record, idempotency);
        return new TransformedMigrationRecord(
            new MigrationRecordWriteRequest(record.TableLogicalName, record.SourceId, record.Attributes, immediateLookups, writeIdempotency),
            deferredPatches);
    }

    private static MigrationWriteIdempotency CreateWriteIdempotency(MigrationRecord record, MigrationTableIdempotency idempotency)
    {
        if (idempotency.Mode == MigrationIdempotencyMode.AlternateKey)
        {
            Dictionary<string, object?> keyValues = new(StringComparer.OrdinalIgnoreCase);
            foreach (string keyField in idempotency.KeyFieldLogicalNames)
            {
                if (!record.Attributes.TryGetValue(keyField, out object? value) || value is null)
                {
                    return MigrationWriteIdempotency.SourceRecordId;
                }

                keyValues[keyField] = value;
            }

            return new MigrationWriteIdempotency(MigrationIdempotencyMode.AlternateKey, keyValues);
        }

        return new MigrationWriteIdempotency(idempotency.Mode, new Dictionary<string, object?>());
    }
}

public sealed record TransformedMigrationRecord(
    MigrationRecordWriteRequest WriteRequest,
    IReadOnlyList<DeferredRelationshipPatch> DeferredPatches);
