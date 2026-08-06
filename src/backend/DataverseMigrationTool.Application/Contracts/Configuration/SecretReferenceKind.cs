namespace DataverseMigrationTool.Application.Contracts.Configuration;

/// <summary>
/// Identifies the supported kinds of non-secret references used to locate sensitive values.
/// </summary>
public enum SecretReferenceKind
{
    /// <summary>
    /// The reference name is an environment variable containing the secret at runtime.
    /// </summary>
    EnvironmentVariable,

    /// <summary>
    /// The reference name is a Key Vault secret name or URI resolved by the hosting environment.
    /// </summary>
    KeyVaultSecret,

    /// <summary>
    /// The reference name is resolved by an external secret provider owned by the host.
    /// </summary>
    ExternalReference
}
