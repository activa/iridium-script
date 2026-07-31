using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using Iridium.Convert;
using Iridium.Script.Reflection;

namespace Iridium.Script;

internal static class ReflectionExtensions
{
    private static readonly ConcurrentDictionary<Type,TypeTraits> _typeTraits = new();

    public static TypeTraits Traits(this Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        return _typeTraits.GetOrAdd(type, t => new TypeTraits(t));
    }

    public static bool IsNullable(this Type type)
    {
        return type.Traits().Is(TypeTraitFlags.Nullable);
    }

    public static bool CanBeNull(this Type type)
    {
        return type.Traits().Is(TypeTraitFlags.CanBeNull);
    }

    public static Type RealType(this Type type)
    {
        return type.Traits().RealType;
    }

    public static PropertyInfo? FindIndexer(this Type type, Type[] types)
    {
        return WalkType(type, t => t.GetProperties(BindingFlags.Instance|BindingFlags.Public).Where(pi => pi.Name == "Item" && SmartBinder.MatchParameters(types, pi.GetIndexParameters()))).FirstOrDefault();
    }

    public static object? Cast(this Type type, object? value)
    {
        if (value == null)
            return null;

        var valueType = value.GetType();

        if (valueType == type)
            return value;

        var conversion = type.ImplicitConversion(valueType);

        if (conversion != null)
            return conversion(value);

        if (type.IsPrimitive && value is char c)
            value = (short)c; // compiler supports char to number casting but framework does not

        return System.Convert.ChangeType(value, type, null);
    }

    private static IEnumerable<T> WalkType<T>(Type type, Func<Type, IEnumerable<T>> f)
    {
        while (type != null)
        {
            foreach (var item in f(type))
            {
                yield return item;
            }

            type = type.BaseType;
        }
    }

    public static bool Is(this Type type, TypeTraitFlags flags)
    {
        var typeInfo = new TypeTraits(type);

        return typeInfo.Is(flags);
    }

}