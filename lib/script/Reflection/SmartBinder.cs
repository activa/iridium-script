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

using Iridium.Convert;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Iridium.Script.Reflection;

internal class SmartBinder
{
    private enum ParameterCompareType
    {
        Exact,
        Assignable,
        Implicit,
        Convertable
    }

    private class ParameterComparer(ParameterCompareType compareType) : IEqualityComparer<Type>
    {
        public bool Equals(Type? sourceType, Type? targetType)
        {
            switch (compareType)
            {
                case ParameterCompareType.Exact:
                    return sourceType == targetType;
                case ParameterCompareType.Assignable:
                    return targetType!.IsAssignableFrom(sourceType);
                case ParameterCompareType.Implicit:
                    return targetType!.GetMethod("op_Implicit", [sourceType!]) != null;
                case ParameterCompareType.Convertable:
                    return targetType == typeof(string);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public int GetHashCode(Type obj) => obj.GetHashCode();
    }

    public static bool MatchParameters(Type[] parameterTypes, ParameterInfo[] parameters)
    {
        var compareTypes = new[] { ParameterCompareType.Exact, ParameterCompareType.Assignable, ParameterCompareType.Implicit, ParameterCompareType.Convertable };

        return compareTypes.Any(compareType => MatchParameters(parameterTypes, parameters, compareType));
    }

    private static bool MatchParameters(Type[] parameterTypes, ParameterInfo[] parameters, ParameterCompareType compareType)
    {
        if (parameterTypes.Length != parameters.Length)
            return false;

        return parameterTypes.Length == 0 || parameterTypes.SequenceEqual(parameters.Select(p => p.ParameterType), new ParameterComparer(compareType));
    }

    public static T? SelectBestMethod<T>(IEnumerable<T> methods, Type[] parameterTypes) where T : MethodBase
    {
        // Evaluate the candidates against increasingly permissive matching rules,
        // so a better (e.g. exact) overload is always preferred over a weaker one
        // (e.g. one only reachable through an implicit conversion). This makes
        // overload resolution independent of the order in which reflection happens
        // to return the members, which is not guaranteed and differs between
        // runtimes.
        var compareTypes = new[] { ParameterCompareType.Exact, ParameterCompareType.Assignable, ParameterCompareType.Implicit, ParameterCompareType.Convertable };

        var candidates = methods as T[] ?? methods.ToArray();

        return compareTypes
            .Select(compareType => candidates.FirstOrDefault(m => MatchParameters(parameterTypes, m.GetParameters(), compareType)))
            .FirstOrDefault(match => match != null);
    }

    private static object?[] ConvertParameters(object?[] parameters, ParameterInfo[] parameterTypes)
    {
        var newParameters = new object?[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            newParameters[i] = parameters[i].Convert(parameterTypes[i].ParameterType);
        }

        return newParameters;
    }

    public static object? Invoke(MethodBase method, object?[] parameters)
    {
        object?[] p = ConvertParameters(parameters, method.GetParameters());

        if (method is ConstructorInfo constructorInfo)
            return constructorInfo.Invoke(p);

        return method.Invoke(null, p);
    }

    public static object? Invoke(MethodBase method, object target, object[] parameters)
    {
        return method.Invoke(target, ConvertParameters(parameters, method.GetParameters()));
    }
}