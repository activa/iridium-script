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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Iridium.Reflection;
using Iridium.Script;

namespace Iridium.Script
{
    public class DynamicObject : IDynamicObject
    {
        private readonly Dictionary<string, object> _dic = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        private readonly LinkedList<object> _objects = new LinkedList<object>();

        public DynamicObject()
        {
            if (GetType() != typeof(DynamicObject))
                _objects.AddFirst(this);
        }

        public DynamicObject(params object[] dataObjects) : this()
        {
            Apply(dataObjects);
        }

        public void Apply(DynamicObject source)
        {
            foreach (string s in source._dic.Keys)
                _dic[s] = source._dic[s];

            foreach (object obj in source._objects)
                _objects.AddLast(obj);
        }

        public void Apply(object obj)
        {
            if (obj is DynamicObject dynamicObject)
                Apply(dynamicObject);
            else
                _objects.AddLast(obj);
        }

        public void Apply(params object[] objects)
        {
            foreach (var obj in objects)
                Apply(obj);
        }

        public DynamicObject Clone()
        {
            DynamicObject newContainer = (DynamicObject)Activator.CreateInstance(GetType());

            newContainer.Apply(this);

            return newContainer;
        }

        private bool HasProperty(string propertyName)
        {
            if (_objects.Count == 0)
                return false;

            return _objects.Any(obj => GetMember(obj.GetType(), propertyName) != null);
        }

        public bool Contains(string key)
        {
            return _dic.ContainsKey(key) || HasProperty(key);
        }

        public bool TryGetValue(string key, out object value)
        {
            return TryGetValue(key, out value, out var type);
        }

        private static MemberInfo GetMember(Type type, string propertyName)
        { 
            MemberInfo[] members = type.GetMember(propertyName);
            //MemberInfo[] members = type.Inspector().GetMember(propertyName);

            if (members.Length == 0)
                return null;

            MemberInfo member = members[0];

            if (member is MethodInfo)
                return member;

            if (members.Length > 1) // Some frameworks sometimes return > 1 member
            {
                foreach (var memberInfo in members)
                    if (memberInfo.DeclaringType == type)
                        member = memberInfo;
            }

            if (member is PropertyInfo or FieldInfo)
                return member;

            return null;
        }

        public bool TryGetValue(out object value, out Type type)
        {
            value = null;
            type = null;

            return false;
        }

        public bool TryGetValue(string propertyName, out object value, out Type type)
        {
            type = typeof(object);

            if (_dic.TryGetValue(propertyName, out value))
            {
                if (value == null)
                    return true;

                type = value.GetType();

                return true;
            }

            if (_objects.Count == 0)
                return false;

            foreach (object dataObject in _objects)
            {
                MemberInfo member = GetMember(dataObject.GetType(), propertyName);

                switch (member)
                {
                    case null:
                        continue;

                    case PropertyInfo propertyInfo:
                        value = propertyInfo.GetValue(dataObject, null);
                        type = propertyInfo.PropertyType;

                        return true;
                    case FieldInfo fieldInfo:
                        value = fieldInfo.GetValue(dataObject);
                        type = fieldInfo.FieldType;

                        return true;
                    case MethodInfo _:
                        value = new InstanceMethod(dataObject.GetType(), propertyName, dataObject);
                        type = typeof(InstanceMethod);

                        return true;
                }
            }

            return false;
        }

        public bool TryGetValue(int index, out object value, out Type type)
        {
            value = null;
            type = null;

            return false;
        }

        public bool IsArray => false;
        public bool IsValue => false;
        public bool IsObject => true;

        public object this[string key]
        {
            get
            {
                if (TryGetValue(key, out var value))
                    return value;

                return null;
            }
            set
            {
                _dic[key] = value;
            }
        }

        public void AddType(Type t)
        {
            this[t.Name] = ContextFactory.CreateType(t);
        }

        public void AddType(string name, Type t)
        {
            this[name] = ContextFactory.CreateType(t);
        }

        public void AddType<T>(string name)
        {
            this[name] = ContextFactory.CreateType(typeof(T));
        }

        public void AddType<T>()
        {
            this[typeof(T).Name] = ContextFactory.CreateType(typeof(T));
        }

        public void AddFunction<T>(string name, string functionName)
        {
            this[name] = ContextFactory.CreateFunction(typeof(T), functionName);
        }

        public void AddFunction<T>(string name, string functionName, T targetObject)
        {
            this[name] = ContextFactory.CreateFunction(typeof(T), functionName, targetObject);
        }

        public void AddFunction(string name, Type type, string functionName)
        {
            this[name] = ContextFactory.CreateFunction(type, functionName);
        }

        public void AddFunction(string name, Type type, string functionName, object targetObject)
        {
            this[name] = ContextFactory.CreateFunction(type, functionName, targetObject);
        }

        public void AddFunction(string name, MethodInfo methodInfo)
        {
            this[name] = ContextFactory.CreateFunction(methodInfo);
        }

        public void AddFunction(string name, MethodInfo methodInfo, object targetObject)
        {
            this[name] = ContextFactory.CreateFunction(methodInfo, targetObject);
        }
    }
}