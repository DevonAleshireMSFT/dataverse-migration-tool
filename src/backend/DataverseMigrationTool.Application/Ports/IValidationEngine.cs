using DataverseMigrationTool.Domain.Entities;
using DataverseMigrationTool.Domain.ValueObjects;

namespace DataverseMigrationTool.Application.Ports;

public interface IValidationEngine
{
    Task<MigrationValidationResult> ValidateAsync(
        MigrationJob job,
        CancellationToken cancellationToken = default);
}

