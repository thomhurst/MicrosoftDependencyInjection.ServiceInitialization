using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using TomLonghurst.Microsoft.Extensions.DependencyInjection.ServiceInitialization.Extensions;

namespace TomLonghurst.Microsoft.Extensions.DependencyInjection.ServiceInitialization.AspNetCore.Tests;

public class ScopedTests
{
    [Test]
    public async Task Test1()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddScoped<ISomeInterface, SomeClass>()
            .AddScoped<SomeClass2>();

        var serviceProvider = serviceCollection.BuildServiceProvider().CreateAsyncScope().ServiceProvider;

        await serviceProvider.InitializeAsync();
        
        Assert.Multiple(() =>
        {
            Assert.That(serviceProvider.GetRequiredService<ISomeInterface>().InitializeCount, Is.EqualTo(1));
            Assert.That(serviceProvider.GetRequiredService<SomeClass2>().InitializeCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Test_WithInitializers_AddedToCollection()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddScoped<ISomeInterface, SomeClass>()
            .AddInitializers()
            .AddScoped<SomeClass2>();

        var serviceProvider = serviceCollection.BuildServiceProvider().CreateAsyncScope().ServiceProvider;

        await serviceProvider.InitializeAsync();
        
        Assert.Multiple(() =>
        {
            Assert.That(serviceProvider.GetRequiredService<ISomeInterface>().InitializeCount, Is.EqualTo(1));
            Assert.That(serviceProvider.GetRequiredService<SomeClass2>().InitializeCount, Is.EqualTo(1));
        });
    }
    
    [Test]
    public async Task Test_WithFactoryMethods()
    {
        var services = new ServiceCollection();

        services.AddScoped<ISomeInterface>(sp => new SomeClass())
            .AddScoped(sp => new SomeClass2());

        var serviceProvider = services.BuildServiceProvider().CreateAsyncScope().ServiceProvider;
        
        await serviceProvider.InitializeAsync();
        
        Assert.Multiple(() =>
        {
            Assert.That(serviceProvider.GetRequiredService<ISomeInterface>().InitializeCount, Is.EqualTo(1));
            Assert.That(serviceProvider.GetRequiredService<SomeClass2>().InitializeCount, Is.EqualTo(1));
        });
    }
}