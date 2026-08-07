using DataverseMigrationTool.Application.Ports;
using Microsoft.Extensions.DependencyInjection;

namespace DataverseMigrationTool.Infrastructure.Validation;

public static class ValidationServiceCollectionExtensions
{
    public static IServiceCollection AddValidationEngine(this IServiceCollection services)
    {
        services.AddSingleton<IValidationRule, DataverseConnectivityValidationRule>();
        services.AddSingleton<IValidationEngine, RuleBasedValidationEngine>();

        return services;
    }
}
