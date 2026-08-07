using DataverseMigrationTool.Application.Ports;
using DataverseMigrationTool.Domain.Entities;
using DataverseMigrationTool.Domain.ValueObjects.Validation;

namespace DataverseMigrationTool.Infrastructure.Validation;

public sealed class PlaceholderValidationEngine(IDataverseProvider dataverseProvider) : IValidationEngine
{
    private readonly RuleBasedValidationEngine engine = new(
        [new DataverseConnectivityValidationRule(dataverseProvider)]);

    public Task<ValidationReport> ValidateAsync(
        MigrationJob job,
        CancellationToken cancellationToken = default)
        => engine.ValidateAsync(job, cancellationToken);
}
