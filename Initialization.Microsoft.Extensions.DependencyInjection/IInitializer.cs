namespace Initialization.Microsoft.Extensions.DependencyInjection;

public interface IInitializer
{
    Task InitializeAsync();
    
#if NET6_0_OR_GREATER
    int Order => 0;
#else
    int Order { get; }
#endif
}