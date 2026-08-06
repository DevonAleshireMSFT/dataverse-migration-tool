using DataverseMigrationTool.Domain.Enums;

namespace DataverseMigrationTool.Application.Contracts.Configuration;

/// <summary>
/// Validates migration configuration contracts before they cross application boundaries.
/// </summary>
public static class MigrationConfigurationValidator
{
    /// <summary>
    /// Validates the supplied migration configuration.
    /// </summary>
    /// <param name="configuration">The configuration to validate.</param>
    /// <returns>A list of clear validation errors. An empty list means the configuration is valid.</returns>
    public static IReadOnlyList<string> Validate(MigrationConfiguration? configuration)
    {
        List<string> errors = [];

        if (configuration is null)
        {
            errors.Add($"{MigrationConfiguration.SectionName} configuration is required.");
            return errors;
        }

        ValidateEnvironment(configuration.Source, "Source", errors);
        ValidateEnvironment(configuration.Target, "Target", errors);

        return errors;
    }

    /// <summary>
    /// Throws a validation exception when the supplied configuration is invalid.
    /// </summary>
    /// <param name="configuration">The configuration to validate.</param>
    /// <exception cref="MigrationConfigurationValidationException">Thrown when validation fails.</exception>
    public static void ThrowIfInvalid(MigrationConfiguration? configuration)
    {
        IReadOnlyList<string> errors = Validate(configuration);

        if (errors.Count > 0)
        {
            throw new MigrationConfigurationValidationException(errors);
        }
    }

    private static void ValidateEnvironment(
        DataverseEnvironmentConfiguration? environment,
        string sectionName,
        List<string> errors)
    {
        string path = $"{MigrationConfiguration.SectionName}:{sectionName}";

        if (environment is null)
        {
            errors.Add($"{path} is required.");
            return;
        }

        if (string.IsNullOrWhiteSpace(environment.Name))
        {
            errors.Add($"{path}:Name is required.");
        }

        if (!Uri.TryCreate(environment.Url, UriKind.Absolute, out Uri? url))
        {
            errors.Add($"{path}:Url must be an absolute HTTPS URI.");
        }
        else if (url.Scheme != Uri.UriSchemeHttps)
        {
            errors.Add($"{path}:Url must use HTTPS.");
        }

        if (environment.TenantId == Guid.Empty)
        {
            errors.Add($"{path}:TenantId is required and must be a non-empty GUID.");
        }

        if (!Enum.IsDefined(environment.Cloud))
        {
            errors.Add($"{path}:Cloud must be a supported {nameof(DataverseCloud)} value.");
        }

        if (string.IsNullOrWhiteSpace(environment.ClientId))
        {
            errors.Add($"{path}:ClientId is required.");
        }

        ValidateSecretReference(environment.ClientSecretReference, $"{path}:ClientSecretReference", errors);
    }

    private static void ValidateSecretReference(
        SecretReference? reference,
        string path,
        List<string> errors)
    {
        if (reference is null)
        {
            errors.Add($"{path} is required and must reference a secret by name, not by plaintext value.");
            return;
        }

        if (!Enum.IsDefined(reference.Kind))
        {
            errors.Add($"{path}:Kind must be a supported {nameof(SecretReferenceKind)} value.");
        }

        if (string.IsNullOrWhiteSpace(reference.Name))
        {
            errors.Add($"{path}:Name is required and must point to a secret reference, not a plaintext secret.");
        }
    }
}
