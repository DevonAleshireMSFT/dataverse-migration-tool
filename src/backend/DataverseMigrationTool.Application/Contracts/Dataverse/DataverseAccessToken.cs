namespace DataverseMigrationTool.Application.Contracts.Dataverse;

/// <summary>
/// Represents an access token supplied by the auth handoff seam for supported Dataverse calls.
/// Tokens are secrets and must not be logged, persisted, or exposed on connection sessions.
/// </summary>
/// <param name="Token">The bearer access token for the resolved Dataverse resource.</param>
/// <param name="ExpiresOn">The UTC expiry timestamp for the token.</param>
public sealed record DataverseAccessToken(
    string Token,
    DateTimeOffset ExpiresOn);
