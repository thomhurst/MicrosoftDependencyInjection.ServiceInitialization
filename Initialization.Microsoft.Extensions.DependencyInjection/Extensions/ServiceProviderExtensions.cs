using System.Reflection;
using Initialization.Microsoft.Extensions.DependencyInjection.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace Initialization.Microsoft.Extensions.DependencyInjection.Extensions;

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
        InitializeAsync(serviceProvider).GetAwaiter().GetResult();
    }

    private static IOrderedEnumerable<IGrouping<int, IInitializer>> GetAllInitializerBatches(IServiceProvider serviceProvider)
    {
        var serviceDescriptors = GetServiceDescriptors(serviceProvider).ToList();

        var implementationTypes = serviceDescriptors
            .Where(sd => sd.ImplementationType != null)
            .Where(sd => IsInitializer(sd.ImplementationType!))
            .ToList();

        var implementationInstances = serviceDescriptors
            .Where(sd => sd.ImplementationInstance != null)
            .Where(sd => IsInitializer(sd.ImplementationInstance!))
            .ToList();

        var implementationFactories = serviceDescriptors
            .Where(sd => sd.ImplementationFactory != null)
            .ToList();

        var knownInitializers = implementationInstances.Select(sd => sd)
            .Concat(implementationTypes.Select(sd => sd))
            .Select(sd => GetService(serviceProvider, sd))
            .OfType<IInitializer>()
            .ToList();

        var initializersInFactoryMethods = implementationFactories
            .Where(sd => IsFactoryInitializer(sd.ImplementationFactory!))
            .Select(sd => GetService(serviceProvider, sd))
            .OfType<IInitializer>()
            .ToList();

        return knownInitializers
            .Concat(initializersInFactoryMethods)
            .GroupBy(i => i.Order)
            .OrderBy(i => i.Key);
    }

    private static object? GetService(IServiceProvider serviceProvider, ServiceDescriptor serviceDescriptor)
    {
        if (serviceDescriptor.Lifetime != ServiceLifetime.Singleton)
        {
            throw new Exception(
                $"Service Provider Initializers are only supported for Singletons. {ServiceDescriptorImplementationType()} is {serviceDescriptor.Lifetime}");
        }

        return serviceProvider.GetService(serviceDescriptor.ServiceType);

        string ServiceDescriptorImplementationType()
        {
            var type = serviceDescriptor.ImplementationType ?? serviceDescriptor.ImplementationInstance?.GetType() ?? serviceDescriptor.ServiceType;
            return type.Name;
        }
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

    private static bool IsInitializer(object obj) => obj is IInitializer;
    private static bool IsInitializer(Type type) => typeof(IInitializer).IsAssignableFrom(type);
}