using DataverseMigrationTool.Application.Ports;
using DataverseMigrationTool.Infrastructure.Dataverse;
using DataverseMigrationTool.Infrastructure.Jobs;
using DataverseMigrationTool.Infrastructure.Logging;
using DataverseMigrationTool.Infrastructure.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace DataverseMigrationTool.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IDataverseProvider, ServiceClientDataverseProvider>();
        services.AddSingleton<IMigrationJobStore, InMemoryMigrationJobStore>();
        services.AddSingleton<IValidationEngine, PlaceholderValidationEngine>();
        services.AddSingleton<IOperationLogger, MicrosoftExtensionsOperationLogger>();
        services.AddSingleton<IMigrationEngine, PlaceholderMigrationEngine>();

        return services;
    }
}

