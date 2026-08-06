using DataverseMigrationTool.Application.Contracts.Dataverse;
using DataverseMigrationTool.Domain.ValueObjects;

namespace DataverseMigrationTool.Application.Ports;

/// <summary>
/// Supplies Dataverse access tokens for a resolved environment endpoint.
/// Bobbie's security work owns MSAL, consent, cache, and credential acquisition;
/// Dataverse provider implementations only consume the returned token.
/// </summary>
public interface IDataverseTokenProvider
{
    /// <summary>
    /// Gets an access token for the supplied Dataverse environment and resolved endpoint.
    /// Implementations must honor <see cref="DataverseEndpoint.Scopes"/> and the environment cloud.
    /// </summary>
    /// <param name="environment">The environment that needs a Dataverse token.</param>
    /// <param name="endpoint">The resolved Dataverse resource, Web API endpoint, and token scopes.</param>
    /// <param name="cancellationToken">A token used to cancel token acquisition or retrieval.</param>
    /// <returns>An access token and its expiry. The token must not be logged or persisted.</returns>
    ValueTask<DataverseAccessToken> GetAccessTokenAsync(
        EnvironmentProfile environment,
        DataverseEndpoint endpoint,
        CancellationToken cancellationToken = default);
}
