using DataverseMigrationTool.Application.Contracts.Validation;
using DataverseMigrationTool.Application.Ports;
using DataverseMigrationTool.Domain.ValueObjects;
using DataverseMigrationTool.Domain.ValueObjects.Validation;

namespace DataverseMigrationTool.Infrastructure.Validation;

public sealed class DataverseConnectivityValidationRule(IDataverseProvider dataverseProvider) : IValidationRule
{
    public string RuleId => "DMT-CONNECTIVITY-001";

    public string Category => "Connectivity";

    public async Task<IReadOnlyCollection<ValidationFinding>> EvaluateAsync(
        ValidationContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        List<ValidationFinding> findings = [];

        MigrationValidationResult source =
            await dataverseProvider.ValidateConnectionAsync(context.Job.Source, cancellationToken);
        AddFindings(findings, source, "source", context.Job.Source);

        MigrationValidationResult target =
            await dataverseProvider.ValidateConnectionAsync(context.Job.Target, cancellationToken);
        AddFindings(findings, target, "target", context.Job.Target);

        return findings;
    }

    private void AddFindings(
        List<ValidationFinding> findings,
        MigrationValidationResult result,
        string role,
        EnvironmentProfile environment)
    {
        string target = $"{role}:{environment.Name}";

        foreach (string error in result.Errors)
        {
            findings.Add(new ValidationFinding(
                RuleId,
                error,
                ValidationSeverity.Blocker,
                Category,
                target));
        }

        foreach (string warning in result.Warnings)
        {
            findings.Add(new ValidationFinding(
                RuleId,
                warning,
                ValidationSeverity.Warning,
                Category,
                target));
        }

        if (!result.IsValid && result.Errors.Count == 0)
        {
            findings.Add(new ValidationFinding(
                RuleId,
                $"Dataverse connectivity validation failed for {role} environment '{environment.Name}'.",
                ValidationSeverity.Blocker,
                Category,
                target));
        }
    }
}
