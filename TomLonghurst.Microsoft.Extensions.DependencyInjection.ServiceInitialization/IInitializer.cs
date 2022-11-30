namespace TomLonghurst.Microsoft.Extensions.DependencyInjection.ServiceInitialization;

public interface IInitializer
{
    public Task InitializeAsync();
}