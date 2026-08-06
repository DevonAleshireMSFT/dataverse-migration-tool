using DataverseMigrationTool.Application.Contracts.Dataverse;
using DataverseMigrationTool.Domain.ValueObjects;

namespace DataverseMigrationTool.Application.Ports;

/// <summary>
/// Provides the supported Dataverse connectivity seam used by application services.
/// Implementations must use Microsoft-supported Dataverse Web API or ServiceClient
/// patterns only; undocumented or internal endpoints are outside this contract.
/// </summary>
public interface IDataverseProvider
{
    /// <summary>
    /// Establishes a cancellable provider session for the supplied Dataverse environment.
    /// The returned session must not expose bearer tokens or other secrets.
    /// </summary>
    /// <param name="environment">The Dataverse environment profile, including cloud selection.</param>
    /// <param name="cancellationToken">A token used to cancel token handoff and connection work.</param>
    /// <returns>A non-secret description of the established provider session.</returns>
    Task<DataverseConnectionSession> ConnectAsync(
        EnvironmentProfile environment,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the supported Dataverse WhoAmI operation for a previously established session.
    /// </summary>
    /// <param name="session">The session returned by <see cref="ConnectAsync"/>.</param>
    /// <param name="cancellationToken">A token used to cancel the connectivity check.</param>
    /// <returns>The Dataverse organization, business unit, and user identifiers returned by WhoAmI.</returns>
    Task<DataverseWhoAmIResult> WhoAmIAsync(
        DataverseConnectionSession session,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a cancellable end-to-end connectivity check by resolving the supported endpoint,
    /// consuming a token from <see cref="IDataverseTokenProvider"/>, and executing WhoAmI.
    /// </summary>
    /// <param name="environment">The Dataverse environment profile to check.</param>
    /// <param name="cancellationToken">A token used to cancel the check.</param>
    /// <returns>Actionable diagnostics describing success or failure.</returns>
    Task<DataverseConnectivityCheckResult> CheckConnectivityAsync(
        EnvironmentProfile environment,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that the environment is reachable using supported Dataverse connectivity.
    /// This method adapts <see cref="CheckConnectivityAsync"/> to the migration validation result shape.
    /// </summary>
    /// <param name="environment">The Dataverse environment profile to validate.</param>
    /// <param name="cancellationToken">A token used to cancel validation.</param>
    /// <returns>A validation result with safe, actionable diagnostics.</returns>
    Task<MigrationValidationResult> ValidateConnectionAsync(
        EnvironmentProfile environment,
        CancellationToken cancellationToken = default);
}
