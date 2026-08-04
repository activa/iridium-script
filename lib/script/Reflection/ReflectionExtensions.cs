#region License
//=============================================================================
// Iridium Script - .NET scripting and templating engine 
//
// Copyright (c) 2008-2026 Philippe Leybaert
//
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal 
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in 
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.
//=============================================================================
#endregion

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
        Type? t = type;

        while (t != null)
        {
            foreach (var item in f(type))
            {
                yield return item;
            }

            t = t.BaseType;
        }
    }

    public static bool Is(this Type type, TypeTraitFlags flags)
    {
        var typeInfo = new TypeTraits(type);

        return typeInfo.Is(flags);
    }

}