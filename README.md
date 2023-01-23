## Microsoft Dependency Injection - Service Initializers

I'm sure you've come across a scenario where you have a class that just needs to fetch some data once on startup, and then it can cache that data and re-use it for the lifetime of the application.
This could be things like connection strings, or encryption keys, etc.

If we got this on startup, and then cached it, then when providing that value, we don't need to make it async, as we know it for any subsequent code paths.

This means:

* If your current call-stack isn't async, you don't need to refactor it
* No Async state machine is created, which could (marginally) improve performance
* No delaying the first request to a system, as we don't need to go off and fetch it as this point

## Install via Nuget

 `TomLonghurst.Microsoft.Extensions.DependencyInjection.ServiceInitialization`

## Usage

1. Add the interface `IInitializer` to your class, and implement the `InitializeAsync` method.

e.g.

```csharp
public class KeyvaultConnectionStringProvider : IKeyvaultConnectionStringProvider, IInitializer
{
    public string SomeConnectionString { get; private set; }

    public async Task InitializeAsync()
    {
        SomeConnectionString = await GetConnectionStringFromKeyvault();
    }
}
```

2. In your Startup, or Program, call `.AddInitializers()` on your ServiceCollection. Also make sure your class implementing `IInitializer` has been registered too!

```csharp
services
    .AddSingleton<IKeyvaultConnectionStringProvider, KeyvaultConnectionStringProvider>()
    // .. Add Anything else
    .AddInitializers();
```

3. In your Program, once your Application/ServiceProvider has been 'Built', but before you 'Run' your app, call `.InitializeAsync()` on your ServiceProvider.

e.g.

```csharp
        var builder = WebApplication.CreateBuilder();

        builder.Services
            .AddSingleton<IKeyvaultConnectionStringProvider, KeyvaultConnectionStringProvider>()
            // .. Add Anything else
            .AddInitializers();

        var app = builder.Build();

        await app.Services.InitializeAsync();

        await app.RunAsync();
```

4. Now any other class can simply reference the Get-only property:

```csharp
public async Task DoSomething()
{
    var myDatabaseClient = new DatabaseClient(_keyvaultConnectionStringProvider.SomeConnectionString);
    ...
}
```

5. Done. All your Services that were registered in the container, and implement `IInitializer` will have run, and your application should be ready to run.
