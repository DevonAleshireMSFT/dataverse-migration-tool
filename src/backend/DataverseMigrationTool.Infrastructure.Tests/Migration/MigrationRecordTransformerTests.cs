using DataverseMigrationTool.Application.Contracts.Migration;
using DataverseMigrationTool.Infrastructure.Migration;

namespace DataverseMigrationTool.Infrastructure.Tests.Migration;

public sealed class MigrationRecordTransformerTests
{
    [Fact]
    public void Transform_rewrites_lookup_when_target_mapping_exists()
    {
        Guid sourceAccountId = Guid.NewGuid();
        Guid targetAccountId = Guid.NewGuid();
        MigrationIdMap map = new();
        map.Record("account", sourceAccountId, targetAccountId);
        MigrationRecord record = new(
            "contact",
            Guid.NewGuid(),
            new Dictionary<string, object?> { ["firstname"] = "redacted-test" },
            [new MigrationLookupValue("parentcustomerid", "account", sourceAccountId)],
            Array.Empty<MigrationManyToManyLink>());

        TransformedMigrationRecord transformed = new MigrationRecordTransformer().Transform(record, map);

        Assert.Empty(transformed.DeferredPatches);
        Assert.Equal(targetAccountId, transformed.WriteRequest.Lookups["parentcustomerid"].TargetId);
    }

    [Fact]
    public void Transform_defers_lookup_when_target_mapping_is_missing()
    {
        Guid sourceAccountId = Guid.NewGuid();
        Guid sourceContactId = Guid.NewGuid();
        MigrationRecord record = new(
            "contact",
            sourceContactId,
            new Dictionary<string, object?>(),
            [new MigrationLookupValue("parentcustomerid", "account", sourceAccountId)],
            Array.Empty<MigrationManyToManyLink>());

        TransformedMigrationRecord transformed = new MigrationRecordTransformer().Transform(record, new MigrationIdMap());

        Assert.Empty(transformed.WriteRequest.Lookups);
        DeferredRelationshipPatch patch = Assert.Single(transformed.DeferredPatches);
        Assert.Equal(sourceContactId, patch.SourceId);
        Assert.Equal(sourceAccountId, patch.SourceTargetId);
    }
}
