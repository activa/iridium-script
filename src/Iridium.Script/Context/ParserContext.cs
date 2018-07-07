#region License
//=============================================================================
// Iridium Script - Portable .NET Productivity Library 
//
// Copyright (c) 2008-2018 Philippe Leybaert
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
using Iridium.Reflection;
using Iridium.Script;

namespace Iridium.Script
{
    public class ParserContext : IParserContext, IEnumerable<KeyValuePair<string, IValueWithType>>
    {
        private class ValueWithType : IValueWithType
        {
            public ValueWithType(object value)
            {
                Value = value;

                Type = value == null ? typeof(object) : value.GetType();
            }

            public ValueWithType(object value, Type type)
            {
                Value = value;
                Type = type;
            }

            public object Value { get; }
            public Type Type { get; }
        }

        private readonly Dictionary<string, IValueWithType> _variables;

        private readonly IParserContext _parentContext;

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
            RootObject = rootObject;
        }

        public ParserContext(object rootObject) : this()
        {
            RootObject = rootObject;
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
            RootObject = rootObject;

            AddDictionary(dic);
        }

        public ParserContext(object rootObject, IDictionary<string, object> dic, ParserContextBehavior behavior) : this(behavior)
        {
            RootObject = rootObject;

            AddDictionary(dic);
        }

        protected ParserContext(ParserContext parentContext) : this(parentContext.Behavior)
        {
            _parentContext = parentContext;

            AssignmentPermissions = parentContext.AssignmentPermissions;
            StringComparison = parentContext.StringComparison;
            FormatProvider = parentContext.FormatProvider;
        }

        public void AddDictionary(IDictionary<string, object> dic)
        {
            if (dic == null)
                return;

            foreach (var entry in dic)
            {
                _variables[entry.Key] = new ValueWithType(entry.Value);
            }
        }

        public virtual IParserContext CreateLocal(object rootObject = null)
        {
            return new ParserContext(this) { RootObject = rootObject };
        }

        private bool TestBehavior(ParserContextBehavior behavior)
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

        public void SetLocal(string name, object data, Type type)
        {
            _variables[name] = new ValueWithType(data,type);
        }

        public object this[string name]
        {
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

        public void Set(string name, object data, Type type)
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

        public object RootObject { get; set; }

        public virtual bool Exists(string varName)
        {
            if (_variables.ContainsKey(varName))
                return true;

            if (RootObject != null && ObjectMemberExists(RootObject, varName))
                    return true;
            
            if (_parentContext == null || !_parentContext.Exists(varName))
                return false;

            return true;
        }

        public virtual bool Get(string varName, out object value, out Type type)
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
                else if (RootObject != null && TryGetObjectMember(RootObject, varName, out value, out type))
                {
                    return true;
                }
                else
                {
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

        public bool ToBoolean(object value)
        {
            if (value != null)
            {
                if (value is bool @bool)
                    return @bool;

                if (TestBehavior(ParserContextBehavior.ZeroIsFalse))
                {
                    if (value is int || value is uint || value is short || value is ushort || value is long || value is ulong || value is byte || value is sbyte)
                        return Convert.ToInt64(value) != 0;

                    if (value is decimal @decimal)
                        return @decimal != 0m;

                    if (value is float || value is double)
                        return Convert.ToDouble(value) == 0.0;
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

                if (TestBehavior(ParserContextBehavior.NonEmptyStringIsTrue) && (value is string) && ((string) value).Length > 0)
                    return true;

                if (TestBehavior(ParserContextBehavior.EmptyStringIsFalse) && (value is string) && ((string) value).Length == 0)
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

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private static bool TryGetObjectMember(object obj, string propertyName, out object value, out Type type)
        {
            value = null;
            type = typeof(object);

            if (obj is IDynamicObject dynamicObject && dynamicObject.IsObject)
            {
                if (dynamicObject.TryGetValue(propertyName, out value, out type))
                {
                    if (value is IDynamicObject dynField && dynField.IsValue && dynField.TryGetValue(out var fieldValue, out var fieldType))
                    {
                        value = fieldValue;
                        type = fieldType;

                        return true;
                    }

                    return true;
                }

                return false;
            }

            Type targetType = obj.GetType();

            MemberInfo[] members = targetType.Inspector().GetMember(propertyName);

            if (members.Length == 0)
            {
                PropertyInfo indexerPropInfo = targetType.Inspector().GetIndexer(new[] { typeof(string) });

                if (indexerPropInfo != null)
                {
                    value = indexerPropInfo.GetValue(obj, new object[] { propertyName });
                    type = (value != null && indexerPropInfo.PropertyType == typeof(object)) ? value.GetType() : typeof(object);

                    return true;
                }

                return false;
            }

            if (members.Length >= 1 && members[0] is MethodInfo)
            {
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

            var memberInspector = member.Inspector();

            value = memberInspector.GetValue(obj);
            type = memberInspector.Type;

            return true;
        }

        private static bool ObjectMemberExists(object obj, string propertyName)
        {
            return TryGetObjectMember(obj, propertyName, out object value, out var type);
        }
    }
}