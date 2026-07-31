using DataverseMigrationTool.Application.Ports;
using DataverseMigrationTool.Domain.ValueObjects;
using Microsoft.PowerPlatform.Dataverse.Client;

namespace DataverseMigrationTool.Infrastructure.Dataverse;

public sealed class ServiceClientDataverseProvider : IDataverseProvider
{
    public string ClientTypeName => typeof(ServiceClient).FullName ?? nameof(ServiceClient);

    public Task<MigrationValidationResult> ValidateConnectionAsync(
        EnvironmentProfile environment,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(environment);

        return Task.FromResult(MigrationValidationResult.Success);
    }
}

