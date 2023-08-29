using System.Reflection;

namespace TomLonghurst.Microsoft.Extensions.DependencyInjection.ServiceInitialization;

internal static class AssemblyLoadedTypesProvider
{
    public static IEnumerable<Type> GetLoadedTypes()
    {
        return AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(GetLoadableTypes);
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            return e.Types.Where(t => t != null);
        }
    }
}