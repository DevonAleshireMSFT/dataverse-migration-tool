using DataverseMigrationTool.Application.Ports;
using Microsoft.Extensions.DependencyInjection;

namespace DataverseMigrationTool.Infrastructure.Compare;

public static class CompareServiceCollectionExtensions
{
    public static IServiceCollection AddEnvironmentComparison(this IServiceCollection services)
    {
        services.AddSingleton<IMetadataSnapshotComparer, EnvironmentMetadataComparer>();
        services.AddSingleton<IEnvironmentComparisonService, EnvironmentComparisonService>();

        return services;
    }
}
