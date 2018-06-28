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
using Iridium.Core;

namespace Iridium.Script
{
    public class ParserContext : IParserContext, IEnumerable<KeyValuePair<string,IValueWithType>>
    {
        private class ValueWithType : IValueWithType
        {
            public ValueWithType(object value)
            {
                Value = value;

                if (value == null)
                    Type = typeof(object);
                else
                    Type = value.GetType();
            }

            public ValueWithType(object value, Type type)
            {
                Value = value;
                Type = type;
            }

            public object Value { get; set; }
            public Type Type { get; set; }
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

        public void AddJsonObject(JsonObject json)
        {
            if (json == null || !json.IsObject)
                return;

            AddDictionary((Dictionary<string,object>)ConvertJsonObject(json));
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
            if (data is JsonObject jsonObject)
            {
                var o = ConvertJsonObject(jsonObject);

                _variables[name] = new ValueWithType(o);
            }
            else
            {
                _variables[name] = new ValueWithType(data,type);
            }
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

            if (RootObject != null && PropertyHelper.Exists(RootObject, varName))
                    return true;
            
            if (_parentContext == null || !_parentContext.Exists(varName))
                return false;

            return true;
        }

        public virtual bool Get(string varName, out object value, out Type type)
        {
            if (_variables.ContainsKey(varName))
            {
                value = _variables[varName].Value;
                type = _variables[varName].Type;
            }
            else if (RootObject != null && PropertyHelper.TryGetValue(RootObject,varName,out value, out type))
            {
                return true;
            }
            else
            {
                if (_parentContext == null || !_parentContext.Get(varName, out value, out type))
                {
                    value = null;
                    type = typeof(object);

                    return false;
                }
            }

            if (type == typeof(object) && value != null)
                type = value.GetType();

            return true;
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
                    if (value is ICollection)
                        return ((ICollection) value).Count > 0;

                    if (value is IEnumerable)
                    {
                        IEnumerator enumerator = ((IEnumerable) value).GetEnumerator();

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

        private object ConvertJsonObject(JsonObject json)
        {
            if (json.IsArray)
            {
                return json.Select(ConvertJsonObject).ToArray();
            }

            if (json.IsObject)
            {
                return json.Keys.ToDictionary(j => j, j => ConvertJsonObject(json[j]));
            }

            if (json.IsValue)
            {
                return json.Value;
            }

            return null;
        }

        public string Format(string formatString, params object[] parameters)
        {
            return string.Format(FormatProvider, formatString, parameters);
        }

        public IEnumerator<KeyValuePair<string, IValueWithType>> GetEnumerator()
        {
            return _variables.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}