using System;

namespace DeployItSmart.Engine.Utils;

public static class ServiceAccessor
{
    private static Dictionary<Type, object> _services = new();
    public static void Register<T, TImp>(TImp instance) where TImp : T
    {
        _services[typeof(T)] = instance!;
    }

    public static T Use<T>()
    {
        return (T)_services[typeof(T)];
    }
}
