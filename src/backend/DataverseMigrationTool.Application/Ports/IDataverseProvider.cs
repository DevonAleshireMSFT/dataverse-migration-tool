using DataverseMigrationTool.Domain.ValueObjects;

namespace DataverseMigrationTool.Application.Ports;

public interface IDataverseProvider
{
    Task<MigrationValidationResult> ValidateConnectionAsync(
        EnvironmentProfile environment,
        CancellationToken cancellationToken = default);
}

