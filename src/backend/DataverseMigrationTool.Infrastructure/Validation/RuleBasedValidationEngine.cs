using DataverseMigrationTool.Application.Contracts.Validation;
using DataverseMigrationTool.Application.Ports;
using DataverseMigrationTool.Domain.Entities;
using DataverseMigrationTool.Domain.ValueObjects.Validation;

namespace DataverseMigrationTool.Infrastructure.Validation;

public sealed class RuleBasedValidationEngine(IEnumerable<IValidationRule> rules) : IValidationEngine
{
    private readonly IReadOnlyCollection<IValidationRule> rules =
        (rules ?? throw new ArgumentNullException(nameof(rules))).ToArray();

    public async Task<ValidationReport> ValidateAsync(
        MigrationJob job,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);

        ValidationContext context = ValidationContext.ForJob(job);
        List<ValidationFinding> findings = [];

        foreach (IValidationRule rule in rules)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyCollection<ValidationFinding> ruleFindings =
                await rule.EvaluateAsync(context, cancellationToken);

            findings.AddRange(ruleFindings);
        }

        return ValidationReport.FromFindings(findings);
    }
}
