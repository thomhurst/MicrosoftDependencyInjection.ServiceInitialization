using Microsoft.Extensions.DependencyInjection;

namespace Initialization.Microsoft.Extensions.DependencyInjection;

internal class ServiceDescriptorsWrapper
{
    public IList<ServiceDescriptor> ServiceDescriptors { get; }

    public ServiceDescriptorsWrapper(IList<ServiceDescriptor> serviceDescriptors)
    {
        ServiceDescriptors = serviceDescriptors;
    }
}