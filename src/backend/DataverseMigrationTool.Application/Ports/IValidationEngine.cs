using DataverseMigrationTool.Domain.Entities;
using DataverseMigrationTool.Domain.ValueObjects.Validation;

namespace DataverseMigrationTool.Application.Ports;

public interface IValidationEngine
{
    Task<ValidationReport> ValidateAsync(
        MigrationJob job,
        CancellationToken cancellationToken = default);
}
