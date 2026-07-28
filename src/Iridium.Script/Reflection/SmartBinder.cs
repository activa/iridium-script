using Iridium.Convert;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Iridium.Script.Reflection
{
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
            public bool Equals(Type sourceType, Type targetType)
            {
                switch (compareType)
                {
                    case ParameterCompareType.Exact:
                        return sourceType == targetType;
                    case ParameterCompareType.Assignable:
                        return targetType!.IsAssignableFrom(sourceType);
                    case ParameterCompareType.Implicit:
                        return targetType!.GetMethod("op_Implicit", [sourceType]) != null;
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

        public static T SelectBestMethod<T>(IEnumerable<T> methods, Type[] parameterTypes) where T : MethodBase
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

        private static object[] ConvertParameters(object[] parameters, ParameterInfo[] parameterTypes)
        {
            var newParameters = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                newParameters[i] = parameters[i].Convert(parameterTypes[i].ParameterType);
            }

            return newParameters;
        }

        public static object Invoke(MethodBase method, object[] parameters)
        {
            object[] p = ConvertParameters(parameters, method.GetParameters());

            if (method is ConstructorInfo constructorInfo)
                return constructorInfo.Invoke(p);

            return method.Invoke(null, p);
        }

        public static object Invoke(MethodBase method, object target, object[] parameters)
        {
            return method.Invoke(target, ConvertParameters(parameters, method.GetParameters()));
        }
    }
}
