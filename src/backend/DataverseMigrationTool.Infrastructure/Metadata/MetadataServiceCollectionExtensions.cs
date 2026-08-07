using DataverseMigrationTool.Application.Ports;
using Microsoft.Extensions.DependencyInjection;

namespace DataverseMigrationTool.Infrastructure.Metadata;

public static class MetadataServiceCollectionExtensions
{
    public static IServiceCollection AddMetadataDiscovery(this IServiceCollection services)
    {
        services.AddSingleton<IMetadataCache, InMemoryMetadataCache>();
        services.AddSingleton<SupportedDataverseMetadataDiscoveryService>();
        services.AddSingleton<IMetadataDiscoveryService>(serviceProvider =>
        {
            SupportedDataverseMetadataDiscoveryService inner = serviceProvider.GetRequiredService<SupportedDataverseMetadataDiscoveryService>();
            IMetadataCache cache = serviceProvider.GetRequiredService<IMetadataCache>();

            return new CachingMetadataDiscoveryService(inner, cache);
        });

        return services;
    }
}
