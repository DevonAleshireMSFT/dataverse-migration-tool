using DataverseMigrationTool.Application.Contracts.Dataverse;
using DataverseMigrationTool.Application.Ports;
using DataverseMigrationTool.Domain.ValueObjects;

namespace DataverseMigrationTool.Infrastructure.Dataverse;

/// <summary>
/// Temporary auth handoff placeholder until Bobbie's security work wires MSAL and consent.
/// It intentionally acquires no credentials and stores no secrets.
/// </summary>
public sealed class DeferredDataverseTokenProvider : IDataverseTokenProvider
{
    public ValueTask<DataverseAccessToken> GetAccessTokenAsync(
        EnvironmentProfile environment,
        DataverseEndpoint endpoint,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(endpoint);
        cancellationToken.ThrowIfCancellationRequested();

        throw new NotSupportedException(
            "Dataverse token acquisition is deferred to the IDataverseTokenProvider implementation owned by Bobbie's security work (#40).");
    }
}
