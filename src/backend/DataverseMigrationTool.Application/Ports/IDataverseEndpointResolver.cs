using DataverseMigrationTool.Application.Contracts.Dataverse;
using DataverseMigrationTool.Domain.ValueObjects;

namespace DataverseMigrationTool.Application.Ports;

/// <summary>
/// Resolves supported Dataverse endpoints from an environment profile and its cloud selection.
/// The resolver must not assume public cloud for sovereign environments.
/// </summary>
public interface IDataverseEndpointResolver
{
    /// <summary>
    /// Resolves the Dataverse resource, Web API endpoint, authority host, and token scopes.
    /// </summary>
    /// <param name="environment">The environment profile containing the URL and Dataverse cloud.</param>
    /// <returns>The supported endpoint selection for the requested cloud.</returns>
    DataverseEndpoint Resolve(EnvironmentProfile environment);
}
