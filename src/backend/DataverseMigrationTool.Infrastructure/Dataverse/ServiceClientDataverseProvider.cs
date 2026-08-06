using DataverseMigrationTool.Application.Contracts.Dataverse;
using DataverseMigrationTool.Application.Ports;
using DataverseMigrationTool.Domain.ValueObjects;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;

namespace DataverseMigrationTool.Infrastructure.Dataverse;

public sealed class ServiceClientDataverseProvider(
    IDataverseTokenProvider tokenProvider,
    IDataverseEndpointResolver endpointResolver) : IDataverseProvider
{
    public string ClientTypeName => typeof(ServiceClient).FullName ?? nameof(ServiceClient);

    public async Task<DataverseConnectionSession> ConnectAsync(
        EnvironmentProfile environment,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(environment);

        DataverseEndpoint endpoint = endpointResolver.Resolve(environment);
        DataverseAccessToken accessToken = await tokenProvider.GetAccessTokenAsync(environment, endpoint, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        return new DataverseConnectionSession(
            Environment: environment,
            Endpoint: endpoint,
            ConnectedAt: DateTimeOffset.UtcNow,
            TokenExpiresOn: accessToken.ExpiresOn,
            ProviderName: ClientTypeName);
    }

    public async Task<DataverseWhoAmIResult> WhoAmIAsync(
        DataverseConnectionSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        using ServiceClient client = await CreateServiceClientAsync(session.Environment, session.Endpoint, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        WhoAmIResponse response = (WhoAmIResponse)client.Execute(new WhoAmIRequest());

        cancellationToken.ThrowIfCancellationRequested();

        return new DataverseWhoAmIResult(
            UserId: response.UserId,
            BusinessUnitId: response.BusinessUnitId,
            OrganizationId: response.OrganizationId);
    }

    public async Task<DataverseConnectivityCheckResult> CheckConnectivityAsync(
        EnvironmentProfile environment,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(environment);

        DataverseEndpoint endpoint = endpointResolver.Resolve(environment);

        try
        {
            DataverseConnectionSession session = await ConnectAsync(environment, cancellationToken);
            DataverseWhoAmIResult whoAmI = await WhoAmIAsync(session, cancellationToken);

            return DataverseConnectivityCheckResult.Success(endpoint, whoAmI);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException or ArgumentException)
        {
            return DataverseConnectivityCheckResult.Failure(
                endpoint,
                [$"Dataverse connectivity check failed for '{environment.Name}' ({endpoint.EnvironmentUrl}): {ex.Message}"]);
        }
    }

    public async Task<MigrationValidationResult> ValidateConnectionAsync(
        EnvironmentProfile environment,
        CancellationToken cancellationToken = default)
    {
        DataverseConnectivityCheckResult result = await CheckConnectivityAsync(environment, cancellationToken);

        return result.Succeeded
            ? MigrationValidationResult.Success
            : new MigrationValidationResult(false, result.Errors, result.Warnings);
    }

    private async Task<ServiceClient> CreateServiceClientAsync(
        EnvironmentProfile environment,
        DataverseEndpoint endpoint,
        CancellationToken cancellationToken)
    {
        DataverseAccessToken accessToken = await tokenProvider.GetAccessTokenAsync(environment, endpoint, cancellationToken);

        ServiceClient client = new(
            endpoint.EnvironmentUrl,
            _ => Task.FromResult(accessToken.Token),
            useUniqueInstance: true,
            logger: null);

        if (!client.IsReady)
        {
            string detail = string.IsNullOrWhiteSpace(client.LastError)
                ? "ServiceClient was not ready after construction."
                : client.LastError;

            client.Dispose();

            throw new InvalidOperationException(
                $"Dataverse ServiceClient could not connect using supported caller-managed token authentication. {detail}");
        }

        return client;
    }
}
