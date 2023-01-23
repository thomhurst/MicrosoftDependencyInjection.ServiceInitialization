using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using TomLonghurst.Microsoft.Extensions.DependencyInjection.ServiceInitialization.Extensions;

namespace TomLonghurst.Microsoft.Extensions.DependencyInjection.ServiceInitialization.AspNetCore.Tests;

public class Tests
{
    [Test]
    public async Task Test1()
    {
        var builder = WebApplication.CreateBuilder();

        builder.Services.AddSingleton<ISomeInterface, SomeClass>()
            .AddSingleton<SomeClass2>();

        var app = builder.Build();

        await app.Services.InitializeAsync();
        
        Assert.Multiple(() =>
        {
            Assert.That(app.Services.GetRequiredService<ISomeInterface>().InitializeCount, Is.EqualTo(1));
            Assert.That(app.Services.GetRequiredService<SomeClass2>().InitializeCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Test_WithInitializers_AddedToCollection()
    {
        var builder = WebApplication.CreateBuilder();

        builder.Services.AddSingleton<ISomeInterface, SomeClass>()
            .AddInitializers()
            .AddSingleton<SomeClass2>();

        var app = builder.Build();

        await app.Services.InitializeAsync();
        
        Assert.Multiple(() =>
        {
            Assert.That(app.Services.GetRequiredService<ISomeInterface>().InitializeCount, Is.EqualTo(1));
            Assert.That(app.Services.GetRequiredService<SomeClass2>().InitializeCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Test_WithBuildAndInitialize_Extension()
    {
        var services = new ServiceCollection();

        services.AddSingleton<ISomeInterface, SomeClass>()
            .AddSingleton<SomeClass2>();

        var serviceProvider = await services.BuildAndInitializeServicesAsync();
        
        Assert.Multiple(() =>
        {
            Assert.That(serviceProvider.GetRequiredService<ISomeInterface>().InitializeCount, Is.EqualTo(1));
            Assert.That(serviceProvider.GetRequiredService<SomeClass2>().InitializeCount, Is.EqualTo(1));
        });
    }
}