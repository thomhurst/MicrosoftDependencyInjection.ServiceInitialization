namespace TomLonghurst.Microsoft.Extensions.DependencyInjection.ServiceInitialization.AspNetCore.Tests;

public class SomeClass : ISomeInterface, IInitializer
{
    public int InitializeCount { get; private set; }

    public Task InitializeAsync()
    {
        InitializeCount++;
        
        return Task.CompletedTask;
    }
}