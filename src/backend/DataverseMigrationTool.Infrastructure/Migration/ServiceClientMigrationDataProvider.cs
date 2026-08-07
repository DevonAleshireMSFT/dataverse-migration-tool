using System.Runtime.CompilerServices;
using DataverseMigrationTool.Application.Contracts.Dataverse;
using DataverseMigrationTool.Application.Contracts.Migration;
using DataverseMigrationTool.Application.Ports;
using DataverseMigrationTool.Domain.ValueObjects;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;

namespace DataverseMigrationTool.Infrastructure.Migration;

public sealed class ServiceClientMigrationDataProvider(
    IDataverseTokenProvider tokenProvider,
    IDataverseEndpointResolver endpointResolver) : IMigrationDataProvider
{
    public async IAsyncEnumerable<MigrationRecord> ExtractRecordsAsync(
        MigrationDataReadRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        using ServiceClient client = await CreateServiceClientAsync(request.Environment, cancellationToken);
        QueryExpression query = new(request.TableLogicalName)
        {
            ColumnSet = new ColumnSet(true),
            PageInfo = new PagingInfo { Count = request.PageSize, PageNumber = 1 }
        };

        bool moreRecords;
        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            EntityCollection page = client.RetrieveMultiple(query);
            foreach (Entity entity in page.Entities)
            {
                yield return ConvertEntity(request.TableLogicalName, entity);
            }

            moreRecords = page.MoreRecords;
            if (moreRecords)
            {
                query.PageInfo.PageNumber++;
                query.PageInfo.PagingCookie = page.PagingCookie;
            }
        }
        while (moreRecords);
    }

    public async Task<IReadOnlyList<MigrationRecordWriteResult>> UpsertBatchAsync(
        EnvironmentProfile target,
        IReadOnlyList<MigrationRecordWriteRequest> records,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(records);
        using ServiceClient client = await CreateServiceClientAsync(target, cancellationToken);
        List<MigrationRecordWriteResult> results = [];

        foreach (MigrationRecordWriteRequest record in records)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                Entity entity = ToEntity(record);
                UpsertResponse response = (UpsertResponse)client.Execute(new UpsertRequest { Target = entity });
                results.Add(new MigrationRecordWriteResult(record.TableLogicalName, record.SourceId, response.Target?.Id ?? entity.Id, true, null));
            }
            catch (Exception ex) when (ex is InvalidOperationException or TimeoutException or System.ServiceModel.FaultException<OrganizationServiceFault>)
            {
                results.Add(new MigrationRecordWriteResult(
                    record.TableLogicalName,
                    record.SourceId,
                    null,
                    false,
                    new MigrationExecutionError(record.TableLogicalName, record.SourceId, ex.GetType().Name, "Dataverse upsert failed for a record.", IsRetryable(ex), "Review secure server diagnostics, correct data or permissions if terminal, then retry the failed batch.", 0)));
            }
        }

        return results;
    }

    public async Task<IReadOnlyList<MigrationExecutionError>> PatchRelationshipsAsync(
        EnvironmentProfile target,
        IReadOnlyList<MigrationRelationshipPatchRequest> patches,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(patches);
        using ServiceClient client = await CreateServiceClientAsync(target, cancellationToken);
        List<MigrationExecutionError> errors = [];

        foreach (MigrationRelationshipPatchRequest patch in patches)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                Entity entity = new(patch.TableLogicalName, patch.TargetId);
                entity[patch.FieldLogicalName] = new EntityReference(patch.Lookup.TargetTableLogicalName, patch.Lookup.TargetId);
                client.Update(entity);
            }
            catch (Exception ex) when (ex is InvalidOperationException or TimeoutException or System.ServiceModel.FaultException<OrganizationServiceFault>)
            {
                errors.Add(new MigrationExecutionError(patch.TableLogicalName, null, ex.GetType().Name, "Dataverse relationship patch failed.", IsRetryable(ex), "Correct the missing related record or permissions, then retry relationship patching.", 0));
            }
        }

        return errors;
    }

    private static MigrationRecord ConvertEntity(string tableLogicalName, Entity entity)
    {
        Dictionary<string, object?> attributes = new(StringComparer.OrdinalIgnoreCase);
        List<MigrationLookupValue> lookups = [];

        foreach (KeyValuePair<string, object> attribute in entity.Attributes)
        {
            if (attribute.Value is EntityReference reference)
            {
                lookups.Add(new MigrationLookupValue(attribute.Key, reference.LogicalName, reference.Id));
                continue;
            }

            attributes[attribute.Key] = attribute.Value;
        }

        return new MigrationRecord(tableLogicalName, entity.Id, attributes, lookups, Array.Empty<MigrationManyToManyLink>());
    }

    private static Entity ToEntity(MigrationRecordWriteRequest record)
    {
        Entity entity = new(record.TableLogicalName, record.SourceId);
        foreach (KeyValuePair<string, object?> attribute in record.Attributes)
        {
            entity[attribute.Key] = attribute.Value;
        }

        foreach (KeyValuePair<string, MigrationTargetLookupValue> lookup in record.Lookups)
        {
            entity[lookup.Key] = new EntityReference(lookup.Value.TargetTableLogicalName, lookup.Value.TargetId);
        }

        return entity;
    }

    private async Task<ServiceClient> CreateServiceClientAsync(EnvironmentProfile environment, CancellationToken cancellationToken)
    {
        DataverseEndpoint endpoint = endpointResolver.Resolve(environment);
        DataverseAccessToken accessToken = await tokenProvider.GetAccessTokenAsync(environment, endpoint, cancellationToken);
        ServiceClient client = new(endpoint.EnvironmentUrl, _ => Task.FromResult(accessToken.Token), useUniqueInstance: true, logger: null);
        if (!client.IsReady)
        {
            string detail = string.IsNullOrWhiteSpace(client.LastError) ? "ServiceClient was not ready." : client.LastError;
            client.Dispose();
            throw new InvalidOperationException($"Dataverse ServiceClient could not connect for migration execution. {detail}");
        }

        return client;
    }

    private static bool IsRetryable(Exception ex) => ex is TimeoutException;
}
