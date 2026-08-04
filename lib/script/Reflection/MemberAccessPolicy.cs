using System;
using System.Linq;
using System.Reflection;

namespace Iridium.Script;

/// <summary>
/// Decides whether an untrusted script may read or call a reflected member.
/// <para/>
/// What makes a member dangerous is usually not what it does but what it hands
/// back. A single <see cref="Type"/>, <see cref="Assembly"/> or <see cref="Module"/>
/// is enough to walk to any type loaded in the process and call anything on it, so
/// <c>typeof(int).Module.GetType("System.Environment")</c> is already a complete
/// escape from the script sandbox. Blocking that by name is hopeless: there are many
/// routes (<c>Assembly</c>, <c>Module</c>, <c>BaseType</c>, <c>DeclaringType</c>,
/// <c>ReflectedType</c>, <c>GetInterfaces</c>, ...) and they all lead to the same
/// place.
/// <para/>
/// So instead of naming the doors, this treats the whole reflection object model as
/// out of bounds: every member declared on it, and every member of an ordinary object
/// that would hand a piece of it out. What remains available is the data and
/// behaviour of the host's own objects, which is what a script is meant to work with.
/// </summary>
public static class MemberAccessPolicy
{
    /// <summary>
    /// Returns <c>true</c> when <paramref name="member"/> is safe for a script to
    /// access, and <c>false</c> when it would open a door into the reflection and
    /// type loading system.
    /// <para/>
    /// This covers the escape routes that exist for any .NET object. It deliberately
    /// says nothing about whether a given host type belongs in a script's reach:
    /// that is the host's call, made by choosing what it puts in the context.
    /// </summary>
    public static bool IsSafe(MemberInfo? member)
    {
        if (member == null)
            return false;

        // Once a script holds a reflection object, every member of it leads somewhere
        // worse (Type.Module, Type.BaseType, MethodBase.Invoke, Assembly.GetType, ...),
        // so the entire surface of those types is denied rather than single members.
        if (IsReflectionSurface(member.DeclaringType) || IsReflectionSurface(member.ReflectedType))
            return false;

        // Deny the members that turn an ordinary object into a reflection object.
        // object.GetType() is the obvious one, but a host type is equally free to
        // declare a property or field of type Type, or a method returning one.
        return member switch
        {
            MethodInfo method => IsSafeSignature(method),
            PropertyInfo property => !IsReflectionSurface(property.PropertyType),
            FieldInfo field => !IsReflectionSurface(field.FieldType),
            EventInfo @event => !IsReflectionSurface(@event.EventHandlerType),

            // Anything else (a constructor, a nested type) is not something a script
            // needs to reach through member access, so it is denied by default.
            _ => false
        };
    }

    private static bool IsSafeSignature(MethodInfo method)
    {
        if (IsReflectionSurface(method.ReturnType))
            return false;

        // Also refuse methods that take reflection objects. Nothing should be able to
        // feed a Type back into the host, and it stops a method from being used as a
        // laundering step for one.
        return method.GetParameters().All(parameter => !IsReflectionSurface(parameter.ParameterType));
    }

    /// <summary>
    /// Whether <paramref name="type"/> is part of (or carries) the reflection and type
    /// loading machinery.
    /// </summary>
    private static bool IsReflectionSurface(Type? type)
    {
        if (type == null)
            return false;

        // Judge Type[] and ref/out Type the same as Type.
        while (type.IsArray || type.IsByRef || type.IsPointer)
        {
            Type? elementType = type.GetElementType();

            if (elementType == null)
                break;

            type = elementType;
        }

        if (IsReflectionType(type))
            return true;

        // A collection is as good as its contents: IEnumerable<Type> hands out types.
        return type.IsGenericType && type.GetGenericArguments().Any(IsReflectionSurface);
    }

    private static bool IsReflectionType(Type type)
    {
        // Type derives from MemberInfo, so this single check covers Type and TypeInfo
        // along with MethodInfo, ConstructorInfo, FieldInfo, PropertyInfo and
        // EventInfo - and the runtime's own subclasses such as RuntimeType, which is
        // what an expression actually yields at runtime.
        if (typeof(MemberInfo).IsAssignableFrom(type))
            return true;

        if (typeof(Assembly).IsAssignableFrom(type))
            return true;

        if (typeof(Module).IsAssignableFrom(type))
            return true;

        if (typeof(ParameterInfo).IsAssignableFrom(type))
            return true;

        // A delegate exposes the method it points at, and can invoke it.
        if (typeof(Delegate).IsAssignableFrom(type))
            return true;

        // Loading, activating and marshalling: the rest of the escape machinery.
        if (typeof(AppDomain).IsAssignableFrom(type))
            return true;

        if (type == typeof(Activator) || type == typeof(TypedReference) || type == typeof(RuntimeTypeHandle) || type == typeof(RuntimeMethodHandle) || type == typeof(RuntimeFieldHandle))
            return true;

        return IsReflectionNamespace(type.Namespace);
    }

    private static bool IsReflectionNamespace(string? nameSpace)
    {
        if (nameSpace == null)
            return false;

        return nameSpace == "System.Reflection" || nameSpace.StartsWith("System.Reflection.", StringComparison.Ordinal)
            || nameSpace == "System.Runtime.CompilerServices" || nameSpace.StartsWith("System.Runtime.CompilerServices.", StringComparison.Ordinal)
            || nameSpace == "System.Runtime.InteropServices" || nameSpace.StartsWith("System.Runtime.InteropServices.", StringComparison.Ordinal)
            || nameSpace == "System.Runtime.Loader" || nameSpace.StartsWith("System.Runtime.Loader.", StringComparison.Ordinal);
    }
}
