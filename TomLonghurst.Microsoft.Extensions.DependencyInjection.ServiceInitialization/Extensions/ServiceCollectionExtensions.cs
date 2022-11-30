using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace TomLonghurst.Microsoft.Extensions.DependencyInjection.ServiceInitialization.Extensions;

public static class ServiceCollectionExtensions
{
    public static async Task<IServiceProvider> BuildAndInitializeServicesAsync(this IServiceCollection serviceCollection)
    {
        var serviceProvider = new InitializableServiceProvider(serviceCollection.BuildServiceProvider(), serviceCollection);

        await serviceProvider.InitializeAsync();

        return serviceProvider;
    }

    public static IServiceCollection AddInitializers(this IServiceCollection serviceCollection)
    {
        serviceCollection.TryAddSingleton(new ServiceDescriptorsWrapper(serviceCollection));
        return serviceCollection;
    }
}