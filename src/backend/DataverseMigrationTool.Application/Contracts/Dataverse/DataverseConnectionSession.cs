using DataverseMigrationTool.Domain.ValueObjects;

namespace DataverseMigrationTool.Application.Contracts.Dataverse;

/// <summary>
/// Non-secret description of an established Dataverse provider session.
/// The session intentionally excludes bearer tokens and credentials.
/// </summary>
/// <param name="Environment">The environment used for the session.</param>
/// <param name="Endpoint">The resolved Dataverse endpoint selection.</param>
/// <param name="ConnectedAt">The UTC time the provider established the session.</param>
/// <param name="TokenExpiresOn">The UTC expiry of the token consumed to establish the session.</param>
/// <param name="ProviderName">The provider implementation that established the session.</param>
public sealed record DataverseConnectionSession(
    EnvironmentProfile Environment,
    DataverseEndpoint Endpoint,
    DateTimeOffset ConnectedAt,
    DateTimeOffset TokenExpiresOn,
    string ProviderName);
