namespace DataverseMigrationTool.Application.Contracts.Dataverse;

/// <summary>
/// Safe, actionable diagnostics from a Dataverse connectivity check.
/// </summary>
/// <param name="Succeeded">Whether the provider reached Dataverse and completed WhoAmI.</param>
/// <param name="Endpoint">The resolved Dataverse endpoint selection.</param>
/// <param name="WhoAmI">The WhoAmI result when the check succeeds.</param>
/// <param name="Errors">Safe failure diagnostics that do not include secrets.</param>
/// <param name="Warnings">Safe warnings discovered during endpoint resolution or connectivity.</param>
public sealed record DataverseConnectivityCheckResult(
    bool Succeeded,
    DataverseEndpoint Endpoint,
    DataverseWhoAmIResult? WhoAmI,
    IReadOnlyCollection<string> Errors,
    IReadOnlyCollection<string> Warnings)
{
    /// <summary>
    /// Creates a successful connectivity result.
    /// </summary>
    public static DataverseConnectivityCheckResult Success(
        DataverseEndpoint endpoint,
        DataverseWhoAmIResult whoAmI,
        IReadOnlyCollection<string>? warnings = null) =>
        new(
            Succeeded: true,
            Endpoint: endpoint,
            WhoAmI: whoAmI,
            Errors: Array.Empty<string>(),
            Warnings: warnings ?? Array.Empty<string>());

    /// <summary>
    /// Creates a failed connectivity result with safe diagnostics.
    /// </summary>
    public static DataverseConnectivityCheckResult Failure(
        DataverseEndpoint endpoint,
        IReadOnlyCollection<string> errors,
        IReadOnlyCollection<string>? warnings = null) =>
        new(
            Succeeded: false,
            Endpoint: endpoint,
            WhoAmI: null,
            Errors: errors,
            Warnings: warnings ?? Array.Empty<string>());
}
