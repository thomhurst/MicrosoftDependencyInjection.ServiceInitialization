using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using TomLonghurst.Microsoft.Extensions.DependencyInjection.ServiceInitialization.Exceptions;

namespace TomLonghurst.Microsoft.Extensions.DependencyInjection.ServiceInitialization.Extensions;

public static class ServiceProviderExtensions
{
    public static async Task InitializeAsync(this IServiceProvider serviceProvider)
    {
        var isScopedServiceProvider = serviceProvider is not ServiceProvider;
        
        var initializersBatch = GetAllInitializerBatches(serviceProvider, isScopedServiceProvider);

        foreach (var initializers in initializersBatch)
        {
            await Task.WhenAll(initializers.Select(initializer => initializer.InitializeAsync()));
        }
    }
    
    public static void Initialize(this IServiceProvider serviceProvider)
    {
        InitializeAsync(serviceProvider).GetAwaiter().GetResult();
    }

    private static IOrderedEnumerable<IGrouping<int, IInitializer>> GetAllInitializerBatches(
        IServiceProvider serviceProvider, bool isScopedServiceProvider)
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

        var knownInitializers = implementationInstances.Select(sd => sd)
            .Concat(implementationTypes.Select(sd => sd))
            .Select(sd => GetService(serviceProvider, sd, isScopedServiceProvider))
            .OfType<IInitializer>();

        var initializersInFactoryMethods = implementationFactories
            .Where(sd => IsFactoryInitializer(sd.ImplementationFactory!))
            .Select(sd => GetService(serviceProvider, sd, isScopedServiceProvider))
            .OfType<IInitializer>();

        return knownInitializers
            .Concat(initializersInFactoryMethods)
            .GroupBy(x => x.GetType())
            .Select(x => x.First())
            .GroupBy(i => i.Order)
            .OrderBy(i => i.Key);
    }

    private static object? GetService(IServiceProvider serviceProvider, ServiceDescriptor serviceDescriptor,
        bool isScopedServiceProvider)
    {
        if (!isScopedServiceProvider && serviceDescriptor.Lifetime == ServiceLifetime.Scoped)
        {
            return null;
        }
        
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