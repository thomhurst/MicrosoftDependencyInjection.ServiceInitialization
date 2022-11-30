namespace TomLonghurst.Microsoft.Extensions.DependencyInjection.ServiceInitialization.Exceptions;

public class InitializersNotAddedException : DependencyInjectionException
{
    public InitializersNotAddedException() : base("You must call `.AddInitializers()` on your ServiceCollection")
    {
    }
}