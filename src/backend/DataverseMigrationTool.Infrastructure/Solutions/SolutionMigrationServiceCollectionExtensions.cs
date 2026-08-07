using DataverseMigrationTool.Application.Ports;
using Microsoft.Extensions.DependencyInjection;

namespace DataverseMigrationTool.Infrastructure.Solutions;

public static class SolutionMigrationServiceCollectionExtensions
{
    public static IServiceCollection AddSolutionMigrationOrchestration(this IServiceCollection services)
    {
        services.AddSingleton<SupportedSolutionPreflightPolicy>();
        services.AddSingleton<ISolutionMigrationRunStore, InMemorySolutionMigrationRunStore>();
        services.AddSingleton<ISolutionAlmProvider, ServiceClientSolutionAlmProvider>();
        services.AddSingleton<ISolutionExportImportOrchestrator, SolutionExportImportOrchestrator>();
        return services;
    }
}
