using DataverseMigrationTool.Application.Ports;
using DataverseMigrationTool.Infrastructure.Dataverse;
using DataverseMigrationTool.Infrastructure.Dataverse.Auth;
using DataverseMigrationTool.Infrastructure.Jobs;
using DataverseMigrationTool.Infrastructure.Logging;
using DataverseMigrationTool.Infrastructure.Compare;
using DataverseMigrationTool.Infrastructure.Metadata;
using DataverseMigrationTool.Infrastructure.Migration;
using DataverseMigrationTool.Infrastructure.Solutions;
using DataverseMigrationTool.Infrastructure.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace DataverseMigrationTool.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IDataverseEndpointResolver, DefaultDataverseEndpointResolver>();
        services.AddSingleton<DataverseAuthorityResolver>();
        services.AddSingleton<IDataverseDeviceCodePrompt, RejectingDataverseDeviceCodePrompt>();
        services.AddSingleton<IDataverseTokenProvider, MsalDataverseTokenProvider>();
        services.AddSingleton<IDataverseProvider, ServiceClientDataverseProvider>();
        services.AddSingleton<IMigrationJobStore, InMemoryMigrationJobStore>();
        services.AddSingleton<IOperationLogger, MicrosoftExtensionsOperationLogger>();

        services.AddMetadataDiscovery();
        services.AddValidationEngine();
        services.AddEnvironmentComparison();
        services.AddMigrationExecution();
        services.AddSolutionMigrationOrchestration();

        return services;
    }
}

