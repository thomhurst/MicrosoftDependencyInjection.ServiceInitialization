using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using TomLonghurst.Microsoft.Extensions.DependencyInjection.ServiceInitialization.Exceptions;

namespace TomLonghurst.Microsoft.Extensions.DependencyInjection.ServiceInitialization.Extensions;

public static class ServiceProviderExtensions
{
    public static async Task InitializeAsync(this IServiceProvider serviceProvider)
    {
        var initializersBatch = GetAllInitializerBatches(serviceProvider);

        foreach (var initializers in initializersBatch)
        {
            await Task.WhenAll(initializers.Select(initializer => initializer.InitializeAsync()));
        }
    }
    
    public static void Initialize(this IServiceProvider serviceProvider)
    {
        var initializersBatch = GetAllInitializerBatches(serviceProvider);

        foreach (var initializers in initializersBatch)
        {
            Task.WaitAll(initializers.Select(initializer => initializer.InitializeAsync()).ToArray());
        }
    }

    private static IOrderedEnumerable<IGrouping<int, IInitializer>> GetAllInitializerBatches(IServiceProvider serviceProvider)
    {
        var serviceDescriptors = GetServiceDescriptors(serviceProvider).ToArray();
        
        var implementationTypes = serviceDescriptors
            .Where(sd => sd.ImplementationType != null)
            .Where(sd => IsInitializer(sd.ImplementationType!));

        var implementationInstances = serviceDescriptors
            .Where(sd => sd.ImplementationInstance != null)
            .Where(sd => IsInitializer(sd.ImplementationInstance!));

        var implementationFactories = serviceDescriptors
            .Where(sd => sd.ImplementationFactory != null);

        var knownInitializers = implementationInstances.Select(sd => sd.ServiceType)
            .Concat(implementationTypes.Select(sd => sd.ServiceType))
            .Select(serviceProvider.GetService)
            .Cast<IInitializer>();

        var initializersInFactoryMethods = implementationFactories
            .Select(sd => serviceProvider.GetService(sd.ServiceType))
            .OfType<IInitializer>();

        return knownInitializers
            .Concat(initializersInFactoryMethods)
            .GroupBy(x => x.GetType())
            .Select(x => x.First())
            .GroupBy(i => i.Order)
            .OrderBy(i => i.Key);
    }

    private static IEnumerable<ServiceDescriptor> GetServiceDescriptors(IServiceProvider serviceProvider)
    {
        if (serviceProvider is InitializableServiceProvider initializableServiceProvider)
        {
            return initializableServiceProvider.ServiceDescriptors;
        }

        var serviceDescriptorsWrapper = serviceProvider.GetService<ServiceDescriptorsWrapper>();
        
        if (serviceDescriptorsWrapper != null)
        {
            return serviceDescriptorsWrapper.ServiceDescriptors;
        }
        
        var callSiteFactory= typeof(ServiceProvider)
            .GetProperty("CallSiteFactory", BindingFlags.Instance | BindingFlags.NonPublic)?
            .GetValue(serviceProvider);

        if (callSiteFactory?.GetType()
                .GetField("_descriptors", BindingFlags.Instance | BindingFlags.NonPublic)?
                .GetValue(callSiteFactory) is not ServiceDescriptor[] descriptors)
        {
            throw new InitializersNotAddedException();
        }

        return descriptors;
    }

    private static bool IsInitializer(object obj) => obj is IInitializer;
    private static bool IsInitializer(Type type) => type.GetInterfaces().Contains(typeof(IInitializer));
}