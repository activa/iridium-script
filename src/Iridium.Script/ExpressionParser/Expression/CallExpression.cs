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
using System.Reflection;
using Iridium.Reflection;
using Iridium.Script;

namespace Iridium.Script
{
    public class CallExpression : Expression
    {
        public Expression MethodExpression { get; }
        public Expression[] Parameters { get; }

        public CallExpression(Expression methodExpression, Expression[] parameters)
        {
            MethodExpression = methodExpression;
            Parameters = parameters;
        }

        public override ValueExpression Evaluate(IParserContext context)
        {
            object methodObject = MethodExpression.Evaluate(context).Value;

            ValueExpression[] parameters = EvaluateExpressionArray(Parameters, context);
            Type[] parameterTypes = parameters.ConvertAll(expr => expr.Type);
            object[] parameterValues = parameters.ConvertAll(expr => expr.Value);

			switch (methodObject)
			{
			    case MethodDefinition methodDefinition:
			        return Exp.Value((methodDefinition).Invoke(parameterTypes, parameterValues, out var returnType), returnType);

			    case ConstructorInfo[] constructors:
			    {
			        MethodBase method = SmartBinder.SelectBestMethod(constructors, parameterTypes, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);

			        if (method == null)
			            throw new ExpressionEvaluationException("No match found for constructor " + constructors[0].Name, this);

			        object value = SmartBinder.Invoke(method, parameterValues);

			        //object value = ((ConstructorInfo)method).Invoke(parameterValues);

			        return Exp.Value( value, method.DeclaringType);
			    }

			    case Delegate[] delegates:
			    {
			        MethodBase[] methods = delegates.ConvertAll<Delegate, MethodBase>(d => d.GetMethodInfo());

			        MethodBase method = SmartBinder.SelectBestMethod(methods, parameterTypes, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);

			        if (method == null)
			            throw new ExpressionEvaluationException("No match found for delegate " + MethodExpression, this);

			        object value = SmartBinder.Invoke(method, delegates[Array.IndexOf(methods, method)].Target, parameterValues);

			        return Exp.Value(value, ((MethodInfo)method).ReturnType);
			    }

			    case Delegate method:
			    {
			        MethodInfo methodInfo = method.GetMethodInfo();

			        object value = methodInfo.Invoke(method.Target, parameterValues);

			        return Exp.Value(value, methodInfo.ReturnType);
			    }

			    case FunctionDefinitionExpression func:
			    {
			        var functionContext = context.CreateLocal();

			        for (int i = 0; i < parameterValues.Length; i++)
			        {
			            functionContext.Set(func.ParameterNames[i], parameterValues[i]);
			        }

			        return func.Body.Evaluate(functionContext);
			    }
			}

            throw new ExpressionEvaluationException(MethodExpression + " is not a function", this);
        }

#if DEBUG
        public override string ToString()
        {
            string[] parameters = Parameters.ConvertAll(expr => expr.ToString());

            return $"({MethodExpression}({String.Join(",", parameters)}))";
        }
#endif
    }
}
