using DataverseMigrationTool.Application.Ports;
using DataverseMigrationTool.Infrastructure.Jobs;
using Microsoft.Extensions.DependencyInjection;

namespace DataverseMigrationTool.Infrastructure.Migration;

public static class MigrationExecutionServiceCollectionExtensions
{
    public static IServiceCollection AddMigrationExecution(this IServiceCollection services)
    {
        services.AddSingleton<MigrationExecutionPlanner>();
        services.AddSingleton<MigrationRecordTransformer>();
        services.AddSingleton<IMigrationRunStore, JsonFileMigrationRunStore>();
        services.AddSingleton<IMigrationDataProvider, ServiceClientMigrationDataProvider>();
        services.AddSingleton<IRollbackGuidanceGenerator, RollbackGuidanceGenerator>();
        services.AddSingleton<IMigrationExecutor, MigrationExecutor>();
        services.AddSingleton<IMigrationEngine, PlaceholderMigrationEngine>();
        return services;
    }
}
