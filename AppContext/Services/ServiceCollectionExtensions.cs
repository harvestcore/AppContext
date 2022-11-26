using AppContext.Services.Interfaces;

using Microsoft.Extensions.DependencyInjection;

namespace AppContext.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddResourcesServices(this IServiceCollection services)
    {
        return services
            .AddSingleton<IConfigurationService, ConfigurationService>()
            .AddSingleton<IDataContextService, DataContextService>();
    }
}
