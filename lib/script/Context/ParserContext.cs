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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
//using Iridium.Reflection;

namespace Iridium.Script;

public class ParserContext : IParserContext, IDebuggableContext, IExecutionLimitedContext, IEnumerable<KeyValuePair<string, IValueWithType>>
{
    private class ValueWithType : IValueWithType
    {
        public ValueWithType(object? value)
        {
            Value = value;

            Type = value == null ? typeof(object) : value.GetType();
        }

        public ValueWithType(object? value, Type type)
        {
            Value = value;
            Type = type;
        }

        public object? Value { get; }
        public Type Type { get; }
    }

    private readonly Dictionary<string, IValueWithType> _variables;
    private readonly List<object> _objects = new List<object>();

    private readonly IParserContext? _parentContext;

    private ExecutionLimits _executionLimits = ExecutionLimits.Default;
    private ExecutionMonitor? _executionMonitor;

    public ParserContextBehavior Behavior { get; }

    public ParserContext(ParserContextBehavior behavior)
    {
        Behavior = behavior;

        if ((behavior & ParserContextBehavior.CaseInsensitiveVariables) == ParserContextBehavior.CaseInsensitiveVariables)
        {
            _variables = new Dictionary<string, IValueWithType>(StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            _variables = new Dictionary<string, IValueWithType>();
        }
    }

    public ParserContext() : this(ParserContextBehavior.Default)
    {
    }

    public ParserContext(object rootObject, ParserContextBehavior behavior) : this(behavior)
    {
        _objects.Add(rootObject);
    }

    public ParserContext(object rootObject) : this()
    {
        _objects.Add(rootObject);
    }

    public ParserContext(IDictionary<string, object> dic) : this()
    {
        AddDictionary(dic);
    }

    public ParserContext(IDictionary<string, object> dic, ParserContextBehavior behavior) : this(behavior)
    {
        AddDictionary(dic);
    }

    public ParserContext(object rootObject, IDictionary<string, object> dic) : this()
    {
        _objects.Add(rootObject);

        AddDictionary(dic);
    }

    public ParserContext(object rootObject, IDictionary<string, object> dic, ParserContextBehavior behavior) : this(behavior)
    {
        _objects.Add(rootObject);

        AddDictionary(dic);
    }

    protected ParserContext(ParserContext parentContext, object? obj = null) : this(parentContext.Behavior)
    {
        _parentContext = parentContext;

        if (obj != null)
            _objects.Add(obj);

        AssignmentPermissions = parentContext.AssignmentPermissions;
        StringComparison = parentContext.StringComparison;
        FormatProvider = parentContext.FormatProvider;
        Debugger = parentContext.Debugger;

        _executionLimits = parentContext._executionLimits;

        // Local scopes share the outer scope's monitor: the limits apply to the
        // execution as a whole, not to each scope separately.
        _executionMonitor = parentContext.ExecutionMonitor;
    }

    /// <summary>
    /// The debugger attached to this execution scope, if any. Set this before
    /// evaluating a script to enable breakpoints/stepping. It automatically
    /// propagates to local (child) scopes created during evaluation.
    /// </summary>
    public IScriptDebugger Debugger { get; set; }

    /// <summary>
    /// The limits enforced while evaluating with this context. Set this before
    /// evaluating a script; changing it during evaluation has no effect on the
    /// scopes already created. Defaults to <see cref="ExecutionLimits.Default"/>;
    /// assign <see cref="ExecutionLimits.None"/> to disable all limits.
    /// </summary>
    public ExecutionLimits ExecutionLimits
    {
        get => _executionLimits;
        set
        {
            _executionLimits = value;

            _executionMonitor = value.CreateMonitor();
        }
    }

    /// <summary>
    /// Tracks the execution running in this scope, or <c>null</c> when nothing is
    /// limited.
    /// </summary>
    public ExecutionMonitor? ExecutionMonitor => _executionMonitor ??= _executionLimits.CreateMonitor();

    public void Merge(ParserContext other)
    {
        foreach (var obj in other._objects)
        {
            _objects.Add(obj);
        }

        foreach (var kvp in other._variables)
        {
            _variables[kvp.Key] = kvp.Value;
        }
    }

    public void Merge(params object[] obj)
    {
        foreach (var o in obj)
        {
            _objects.Add(o);
        }
    }

    public void AddDictionary(IDictionary<string, object>? dic)
    {
        if (dic == null)
            return;

        foreach (var entry in dic)
        {
            _variables[entry.Key] = new ValueWithType(entry.Value);
        }
    }

    public virtual IParserContext CreateLocal()
    {
        return new ParserContext(this);
    }

    public virtual IParserContext CreateLocal(object? obj)
    {
        return new ParserContext(this, obj);
    }

    internal bool TestBehavior(ParserContextBehavior behavior)
    {
        return ((Behavior & behavior) == behavior);
    }

    public AssignmentPermissions AssignmentPermissions { get; set; } = AssignmentPermissions.None;
    public StringComparison StringComparison { get; set; } = StringComparison.Ordinal;
    public IFormatProvider FormatProvider { get; set; } = NumberFormatInfo.InvariantInfo;

    public void SetLocal<T>(string name, T data)
    {
        SetLocal(name, data, typeof (T));
    }

    public void SetLocal(string name, IValueWithType data)
    {
        SetLocal(name, data.Value, data.Type);
    }

    public void SetLocal(string name, object? data, Type type)
    {
        _variables[name] = new ValueWithType(data,type);
    }

    public object? this[string name]
    {
        get
        {
            return Get(name, out var value, out _) ? value : null;
        }
        set
        {
            if (value == null)
            {
                Set(name, null, typeof(object));
            }
            else
            {
                if (value is Type type)
                    AddType(name, type);
                else
                    Set(name, value, value.GetType());
            }
        }
    }

    public void Set(string name, object? data, Type type)
    {
        if (_parentContext != null && _parentContext.Exists(name))
            _parentContext.Set(name, data, type);
        else
            SetLocal(name, data, type);
    }

    public void Add(string name, object data, Type type)
    {
        if (_parentContext != null && _parentContext.Exists(name))
            _parentContext.Set(name, data, type);
        else
            SetLocal(name, data, type);
    }

    public void Add<T>(string name, T data)
    {
        Set(name, data, typeof (T));
    }

    public void Set<T>(string name, T data)
    {
        Set(name, data, typeof (T));
    }

    public void Set(string name, IValueWithType data)
    {
        Set(name, data.Value, data.Type);
    }

    public void Add(string name, IValueWithType data)
    {
        Set(name, data.Value, data.Type);
    }

    public void AddType(string name, Type type)
    {
        Set(name, ContextFactory.CreateType(type));
    }

    public void AddFunction(string name, Type type, string methodName)
    {
        Set(name, ContextFactory.CreateFunction(type, methodName));
    }

    public void AddFunction(string name, Type type, string methodName, object targetObject)
    {
        Set(name, ContextFactory.CreateFunction(type, methodName, targetObject));
    }

    public void AddFunction(string name, MethodInfo methodInfo)
    {
        Set(name, ContextFactory.CreateFunction(methodInfo));
    }

    public void AddFunction(string name, MethodInfo methodInfo, object targetObject)
    {
        Set(name, ContextFactory.CreateFunction(methodInfo, targetObject));
    }

    public virtual bool Exists(string varName)
    {
        if (_variables.ContainsKey(varName))
            return true;

        foreach (var obj in _objects)
        {
            if (ObjectMemberExists(obj, varName))
                return true;
        }
            
        if (_parentContext == null || !_parentContext.Exists(varName))
            return false;

        return true;
    }

    public virtual bool Get(string varName, out object? value, out Type? type)
    {
        type = typeof(object);
        value = null;

        try
        {
            if (_variables.ContainsKey(varName))
            {
                value = _variables[varName].Value;
                type = _variables[varName].Type;

                return true;
            }
            else
            {
                foreach (var obj in _objects)
                {
                    if (TryGetObjectMember(obj, varName, out value, out type))
                        return true;
                }

                if (_parentContext != null && _parentContext.Get(varName, out value, out type))
                    return true;
            }

            return false;
        }
        finally
        {
            if (type == typeof(object) && value != null)
                type = value.GetType();
        }
    }

    public bool ToBoolean(object? value)
    {
        if (value != null)
        {
            if (value is bool @bool)
                return @bool;

            if (TestBehavior(ParserContextBehavior.ZeroIsFalse))
            {
                if (value is int or uint or short or ushort or long or ulong or byte or sbyte)
                    return System.Convert.ToInt64(value) != 0;

                if (value is decimal @decimal)
                    return @decimal != 0m;

                if (value is float or double)
                    return System.Convert.ToDouble(value) == 0.0;
            }

            if (TestBehavior(ParserContextBehavior.EmptyCollectionIsFalse))
            {
                if (value is ICollection collection)
                    return collection.Count > 0;

                if (value is IEnumerable enumerable)
                {
                    IEnumerator enumerator = enumerable.GetEnumerator();

                    if (enumerator.MoveNext())
                        return true;

                    return false;
                }
            }

            if (TestBehavior(ParserContextBehavior.NonEmptyStringIsTrue) && (value is string { Length: > 0 }))
                return true;

            if (TestBehavior(ParserContextBehavior.EmptyStringIsFalse) && (value is string { Length: 0 }))
                return false;

            if (TestBehavior(ParserContextBehavior.NotNullIsTrue))
                return true;
        }
        else
        {
            if (TestBehavior(ParserContextBehavior.NullIsFalse))
                return false;
        }

        if (_parentContext != null)
            return _parentContext.ToBoolean(value);

        if (value == null)
            throw new NullReferenceException();
        else
            throw new ArgumentException("Type " + value.GetType().Name + " cannot be evaluated as boolean");
    }

    public string Format(string formatString, params object[] parameters)
    {
        return String.Format(FormatProvider, formatString, parameters);
    }

    public IEnumerator<KeyValuePair<string, IValueWithType>> GetEnumerator()
    {
        return _variables.GetEnumerator();
    }

    /// <summary>
    /// Enumerates the variables visible from this scope, walking outward through
    /// parent scopes. Variables in inner scopes shadow those with the same name in
    /// outer scopes. Intended for debugger "locals" inspection.
    /// </summary>
    public IEnumerable<KeyValuePair<string, IValueWithType>> GetVariablesInScope()
    {
        var seen = new HashSet<string>(_variables.Comparer);

        for (ParserContext context = this; context != null; context = context._parentContext as ParserContext)
        {
            foreach (var variable in context._variables)
            {
                if (seen.Add(variable.Key))
                    yield return variable;
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private static bool TryGetObjectMember(object obj, string propertyName, out object? value, out Type? type)
    {
        value = null;
        type = typeof(object);

        /*
        if (obj is IDynamicObject { IsObject: true } dynamicObject)
        {
            if (dynamicObject.TryGetValue(propertyName, out value, out type))
            {
                if (value is IDynamicObject { IsValue: true } dynField && dynField.TryGetValue(out var fieldValue, out var fieldType))
                {
                    value = fieldValue;
                    type = fieldType;

                    return true;
                }

                return true;
            }

            return false;
        }
        */

        Type targetType = obj.GetType();

        MemberInfo[] members = targetType.GetMember(propertyName);

        if (members.Length == 0)
        {
            PropertyInfo? indexerPropInfo = targetType.FindIndexer([typeof(string)]);

            if (indexerPropInfo != null)
            {
                if (!MemberAccessPolicy.IsSafe(indexerPropInfo))
                    throw new ExpressionEvaluationException("Access to member " + indexerPropInfo.Name + " is not allowed");

                value = indexerPropInfo.GetValue(obj, [propertyName]);
                type = (value != null && indexerPropInfo.PropertyType == typeof(object)) ? value.GetType() : typeof(object);

                return true;
            }

            return false;
        }

        if (members.Length >= 1 && members[0] is MethodInfo methodInfo)
        {
            if (!MemberAccessPolicy.IsSafe(methodInfo))
                throw new ExpressionEvaluationException("Access to member " + methodInfo.Name + " is not allowed");

            value = new InstanceMethod(targetType, propertyName, obj);
            type = typeof(InstanceMethod);

            return true;
        }

        MemberInfo member = members[0];

        if (members.Length > 1) // CoolStorage, ActiveRecord and Dynamic Proxy frameworks sometimes return > 1 member
        {
            foreach (var memberInfo in members)
                if (memberInfo.DeclaringType == obj.GetType())
                    member = memberInfo;
        }

        if (!MemberAccessPolicy.IsSafe(member))
            throw new ExpressionEvaluationException("Access to member " + member.Name + " is not allowed");

        if (member is FieldInfo fieldInfo)
        {
            value = fieldInfo.GetValue(obj);
            type = fieldInfo.FieldType;
        }
        else if (member is PropertyInfo propertyInfo)
        {
            value = propertyInfo.GetValue(obj);
            type = propertyInfo.PropertyType;
        }
        else
        {
            return false;
        }

        return true;
    }

    private static bool ObjectMemberExists(object obj, string propertyName)
    {
        return TryGetObjectMember(obj, propertyName, out _, out _);
    }
}

internal static class BehaviorExtensions
{
    public static bool HasBehavior(this ParserContextBehavior behavior, ParserContextBehavior test)
    {
        return (behavior & test) == test;
    }
}