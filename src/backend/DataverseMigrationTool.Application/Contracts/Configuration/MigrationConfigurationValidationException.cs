namespace DataverseMigrationTool.Application.Contracts.Configuration;

/// <summary>
/// Represents validation failures in migration configuration contracts.
/// </summary>
public sealed class MigrationConfigurationValidationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MigrationConfigurationValidationException"/> class.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    public MigrationConfigurationValidationException(IReadOnlyList<string> errors)
        : base("Migration configuration is invalid: " + string.Join("; ", errors))
    {
        Errors = errors;
    }

    /// <summary>
    /// Gets the validation errors.
    /// </summary>
    public IReadOnlyList<string> Errors { get; }
}
