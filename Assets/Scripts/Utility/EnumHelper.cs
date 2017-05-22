using System;
using System.Diagnostics;
using Random = UnityEngine.Random;

public static class EnumHelper
{
    public static T GetRandomEnum<T>()
    {
        Debug.Assert(Enum.GetUnderlyingType(typeof(T)) == typeof(int));

        var vals = Enum.GetValues(typeof(T));
        return (T)vals.GetValue(Random.Range(0, vals.Length));
    }
}
