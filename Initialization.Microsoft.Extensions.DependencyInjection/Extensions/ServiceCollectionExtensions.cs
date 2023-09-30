using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Initialization.Microsoft.Extensions.DependencyInjection.Extensions;

public static class ServiceCollectionExtensions
{
    public static async Task<IServiceProvider> BuildAndInitializeServicesAsync(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddInitializers();
        
        var serviceProvider = serviceCollection.BuildServiceProvider();

        await serviceProvider.InitializeAsync();

        return serviceProvider;
    }

    public static IServiceCollection AddInitializers(this IServiceCollection serviceCollection)
    {
        serviceCollection.TryAddSingleton(new ServiceDescriptorsWrapper(serviceCollection));
        return serviceCollection;
    }
}