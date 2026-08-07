using DataverseMigrationTool.Domain.ValueObjects;

namespace DataverseMigrationTool.Application.Contracts.Migration;

public sealed record MigrationRecord(
    string TableLogicalName,
    Guid SourceId,
    IReadOnlyDictionary<string, object?> Attributes,
    IReadOnlyList<MigrationLookupValue> Lookups,
    IReadOnlyList<MigrationManyToManyLink> ManyToManyLinks);

public sealed record MigrationLookupValue(
    string FieldLogicalName,
    string TargetTableLogicalName,
    Guid SourceTargetId);

public sealed record MigrationManyToManyLink(
    string RelationshipSchemaName,
    string TargetTableLogicalName,
    Guid SourceTargetId);

public sealed record MigrationRecordWriteRequest(
    string TableLogicalName,
    Guid SourceId,
    IReadOnlyDictionary<string, object?> Attributes,
    IReadOnlyDictionary<string, MigrationTargetLookupValue> Lookups);

public sealed record MigrationTargetLookupValue(string TargetTableLogicalName, Guid TargetId);

public sealed record MigrationRecordWriteResult(
    string TableLogicalName,
    Guid SourceId,
    Guid? TargetId,
    bool Succeeded,
    MigrationExecutionError? Error);

public sealed record DeferredRelationshipPatch(
    string TableLogicalName,
    Guid SourceId,
    string FieldLogicalName,
    string TargetTableLogicalName,
    Guid SourceTargetId);

public sealed record MigrationRelationshipPatchRequest(
    string TableLogicalName,
    Guid TargetId,
    string FieldLogicalName,
    MigrationTargetLookupValue Lookup);

public sealed record MigrationDataReadRequest(
    EnvironmentProfile Environment,
    string TableLogicalName,
    int PageSize);
