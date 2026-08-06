namespace DataverseMigrationTool.Application.Contracts.Configuration;

/// <summary>
/// Describes where a secret can be resolved without carrying the plaintext secret value.
/// </summary>
public sealed class SecretReference
{
    /// <summary>
    /// Gets or sets the kind of secret reference.
    /// </summary>
    public SecretReferenceKind Kind { get; set; } = SecretReferenceKind.EnvironmentVariable;

    /// <summary>
    /// Gets or sets the environment variable name, Key Vault secret name, or external secret reference.
    /// This value must never be a plaintext secret.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
