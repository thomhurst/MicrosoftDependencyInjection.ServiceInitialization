namespace Initialization.Microsoft.Extensions.DependencyInjection.Exceptions;

public class InitializersNotAddedException : DependencyInjectionException
{
    public InitializersNotAddedException() : base("You must call `.AddInitializers()` on your ServiceCollection")
    {
    }
}