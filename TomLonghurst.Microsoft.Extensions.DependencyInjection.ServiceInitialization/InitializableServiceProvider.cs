using Microsoft.Extensions.DependencyInjection;

namespace TomLonghurst.Microsoft.Extensions.DependencyInjection.ServiceInitialization;

internal class InitializableServiceProvider : IServiceProvider
{
    private readonly IServiceProvider _serviceProviderImplementation;
    
    public IList<ServiceDescriptor> ServiceDescriptors { get; }

    public InitializableServiceProvider(IServiceProvider serviceProviderImplementation, IList<ServiceDescriptor> serviceDescriptors)
    {
        _serviceProviderImplementation = serviceProviderImplementation;
        ServiceDescriptors = serviceDescriptors;
    }

    public object? GetService(Type serviceType)
    {
        return _serviceProviderImplementation.GetService(serviceType);
    }
}