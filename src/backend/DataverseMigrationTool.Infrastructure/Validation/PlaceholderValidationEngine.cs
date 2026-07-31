using DataverseMigrationTool.Application.Ports;
using DataverseMigrationTool.Domain.Entities;
using DataverseMigrationTool.Domain.ValueObjects;

namespace DataverseMigrationTool.Infrastructure.Validation;

public sealed class PlaceholderValidationEngine(IDataverseProvider dataverseProvider) : IValidationEngine
{
    public async Task<MigrationValidationResult> ValidateAsync(
        MigrationJob job,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);

        MigrationValidationResult source = await dataverseProvider.ValidateConnectionAsync(job.Source, cancellationToken);
        MigrationValidationResult target = await dataverseProvider.ValidateConnectionAsync(job.Target, cancellationToken);

        return source.IsValid && target.IsValid
            ? MigrationValidationResult.Success
            : new MigrationValidationResult(false, source.Errors.Concat(target.Errors).ToArray(), source.Warnings.Concat(target.Warnings).ToArray());
    }
}

