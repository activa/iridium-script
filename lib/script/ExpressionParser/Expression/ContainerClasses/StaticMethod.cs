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
using System.Reflection;
using Iridium.Script.Reflection;

namespace Iridium.Script;

internal class StaticMethod : MethodDefinition
{
    public StaticMethod(MethodInfo methodInfo) : base(methodInfo)
    {
    }

    public StaticMethod(Type type, string methodName) : base(type, methodName)
    {
    }

    public override object Invoke(Type[] types, object[] parameters, out Type returnType)
    {
        MethodInfo methodInfo = GetMethodInfo(types);

        if (methodInfo == null)
            throw new MissingMemberException(MethodName);

        returnType = methodInfo.ReturnType;

        // Convert the arguments to the selected overload's parameter types
        // (consistent with InstanceMethod), so e.g. an int argument bound to a
        // method that only exposes a wider numeric parameter is converted rather
        // than causing a reflection ArgumentException.
        return SmartBinder.Invoke(methodInfo, parameters);
    }
}