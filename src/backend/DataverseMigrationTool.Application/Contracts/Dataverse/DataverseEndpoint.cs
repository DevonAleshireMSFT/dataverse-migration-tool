using DataverseMigrationTool.Domain.Enums;

namespace DataverseMigrationTool.Application.Contracts.Dataverse;

/// <summary>
/// Describes the supported Dataverse endpoints selected for an environment.
/// Web API calls should target <see cref="WebApiBaseUrl"/> and token requests should use
/// <see cref="Scopes"/> through the auth handoff seam.
/// </summary>
/// <param name="Cloud">The configured Dataverse cloud for the environment.</param>
/// <param name="EnvironmentUrl">The configured environment URL.</param>
/// <param name="WebApiBaseUrl">The supported Dataverse Web API base URL.</param>
/// <param name="AuthorityHost">The cloud-specific Microsoft Entra authority host.</param>
/// <param name="Resource">The Dataverse resource URI for token acquisition.</param>
/// <param name="Scopes">The delegated or application scopes requested from the token provider.</param>
public sealed record DataverseEndpoint(
    DataverseCloud Cloud,
    Uri EnvironmentUrl,
    Uri WebApiBaseUrl,
    Uri AuthorityHost,
    Uri Resource,
    IReadOnlyCollection<string> Scopes);
