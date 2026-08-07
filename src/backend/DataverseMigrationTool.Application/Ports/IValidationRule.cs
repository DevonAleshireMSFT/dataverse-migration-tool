using DataverseMigrationTool.Application.Contracts.Validation;
using DataverseMigrationTool.Domain.ValueObjects.Validation;

namespace DataverseMigrationTool.Application.Ports;

public interface IValidationRule
{
    string RuleId { get; }

    string Category { get; }

    Task<IReadOnlyCollection<ValidationFinding>> EvaluateAsync(
        ValidationContext context,
        CancellationToken cancellationToken = default);
}
