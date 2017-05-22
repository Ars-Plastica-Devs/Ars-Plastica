using System;
using System.Collections.Generic;
using System.Linq;

public static class ReflectionHelper
{
    public static IEnumerable<T> GetAllInstancesOfAttribute<T>(bool inherited)
        where T : Attribute
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .SelectMany(t => t.GetCustomAttributes(typeof(T), inherited))
            .Cast<T>();
    }

    public static IEnumerable<Type> GetConcreteDescendantTypes<T>()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsSubclassOf(typeof(T)) && !t.IsAbstract);
    }
}
