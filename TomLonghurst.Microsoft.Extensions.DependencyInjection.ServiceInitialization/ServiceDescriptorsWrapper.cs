using Microsoft.Extensions.DependencyInjection;

namespace TomLonghurst.Microsoft.Extensions.DependencyInjection.ServiceInitialization;

internal class ServiceDescriptorsWrapper
{
    public IList<ServiceDescriptor> ServiceDescriptors { get; }

    public ServiceDescriptorsWrapper(IList<ServiceDescriptor> serviceDescriptors)
    {
        ServiceDescriptors = serviceDescriptors;
    }
}