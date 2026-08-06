namespace DataverseMigrationTool.Infrastructure.Dataverse.Auth;

/// <summary>
/// Contains the device-code values that a trusted UI or operator channel must display to the user.
/// These values are time-limited authentication artifacts and must not be written to logs.
/// </summary>
/// <param name="VerificationUri">The Microsoft verification URI where the user enters the code.</param>
/// <param name="UserCode">The time-limited device code to present only through a trusted prompt.</param>
/// <param name="ExpiresOn">The timestamp at which the device code expires.</param>
/// <param name="TenantId">The explicit tenant boundary for the authentication attempt.</param>
/// <param name="EnvironmentName">The environment name associated with the authentication attempt.</param>
public sealed record DataverseDeviceCodePromptContext(
    Uri VerificationUri,
    string UserCode,
    DateTimeOffset ExpiresOn,
    Guid TenantId,
    string EnvironmentName)
{
    /// <inheritdoc />
    public override string ToString() =>
        $"Dataverse device-code prompt for environment '{EnvironmentName}' in tenant '{TenantId}' expires at '{ExpiresOn:O}'. Code redacted.";
}
