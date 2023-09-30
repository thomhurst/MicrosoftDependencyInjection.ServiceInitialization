namespace Initialization.Microsoft.Extensions.DependencyInjection.Exceptions;

public class DependencyInjectionException : Exception
{
    public DependencyInjectionException(string? message) : base(message)
    {
    }

    public DependencyInjectionException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}