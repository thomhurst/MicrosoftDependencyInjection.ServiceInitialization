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
        using var serviceScope = serviceProvider.CreateScope();

        var scopedServiceProvider = serviceScope.ServiceProvider;
        
        var initializersBatch = GetAllInitializerBatches(scopedServiceProvider);

        foreach (var initializers in initializersBatch)
        {
            Task.WaitAll(initializers.Select(initializer => initializer.InitializeAsync()).ToArray());
        }
    }

    private static IOrderedEnumerable<IGrouping<int, IInitializer>> GetAllInitializerBatches(IServiceProvider serviceProvider)
    {
        var serviceDescriptors = GetServiceDescriptors(serviceProvider).ToArray();

        var rootServiceProvider = serviceProvider.GetRootServiceProvider();
        var scopedServiceProvider = serviceProvider.GetScopedServiceProvider();
        
        var implementationTypes = serviceDescriptors
            .Where(sd => sd.ImplementationType != null)
            .Where(sd => IsInitializer(sd.ImplementationType!));

        var implementationInstances = serviceDescriptors
            .Where(sd => sd.ImplementationInstance != null)
            .Where(sd => IsInitializer(sd.ImplementationInstance!));

        var implementationFactories = serviceDescriptors
            .Where(sd => sd.ImplementationFactory != null);

        var knownInitializers = implementationInstances.Select(sd => sd)
            .Concat(implementationTypes.Select(sd => sd))
            .Select(sd => GetService(rootServiceProvider, scopedServiceProvider, sd))
            .OfType<IInitializer>();

        var initializersInFactoryMethods = implementationFactories
            .Where(sd => IsFactoryInitializer(sd.ImplementationFactory!))
            .Select(sd => GetService(rootServiceProvider, scopedServiceProvider, sd))
            .OfType<IInitializer>();

        return knownInitializers
            .Concat(initializersInFactoryMethods)
            .GroupBy(x => x.GetType())
            .Select(x => x.First())
            .GroupBy(i => i.Order)
            .OrderBy(i => i.Key);
    }

    private static object? GetService(IServiceProvider rootServiceProvider, IServiceProvider scopedServiceProvider, ServiceDescriptor serviceDescriptor)
    {
        var serviceProvider = serviceDescriptor.Lifetime == ServiceLifetime.Scoped
            ? scopedServiceProvider
            : rootServiceProvider;
        
        return serviceProvider.GetService(serviceDescriptor.ServiceType);
    }

    private static bool IsFactoryInitializer(Func<IServiceProvider,object> implementationFactory)
    {
        var obj = implementationFactory.Method.ReturnType;

        if (IsInitializer(obj))
        {
            return true;
        }
        
        return AssemblyLoadedTypesProvider.GetLoadedTypes()
            .Where(type => obj.IsAssignableFrom(type))
            .Any(IsInitializer);
    }

    private static IEnumerable<ServiceDescriptor> GetServiceDescriptors(IServiceProvider serviceProvider)
    {
        var serviceDescriptorsWrapper = serviceProvider.GetService<ServiceDescriptorsWrapper>();
        
        if (serviceDescriptorsWrapper != null)
        {
            return serviceDescriptorsWrapper.ServiceDescriptors;
        }
        
        var rootScope = GetRootServiceProvider(serviceProvider);
        
        var callSiteFactory= typeof(ServiceProvider)
            .GetProperty("CallSiteFactory", BindingFlags.Instance | BindingFlags.NonPublic)?
            .GetValue(rootScope);

        if (callSiteFactory?.GetType()
                .GetField("_descriptors", BindingFlags.Instance | BindingFlags.NonPublic)?
                .GetValue(callSiteFactory) is not ServiceDescriptor[] descriptors)
        {
            throw new InitializersNotAddedException();
        }

        return descriptors;
    }

    internal static IServiceProvider GetRootServiceProvider(this IServiceProvider rootScope)
    {
        if (rootScope is not ServiceProvider)
        {
            rootScope = (IServiceProvider) rootScope.GetType()
                .GetProperty("RootProvider", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(rootScope);
        }

        return rootScope;
    }
    
    internal static IServiceProvider GetScopedServiceProvider(this IServiceProvider serviceProvider)
    {
        return serviceProvider is ServiceProvider 
            ? serviceProvider.CreateAsyncScope().ServiceProvider 
            : serviceProvider;
    }

    private static bool IsInitializer(object obj) => obj is IInitializer;
    private static bool IsInitializer(Type type) => typeof(IInitializer).IsAssignableFrom(type);
}