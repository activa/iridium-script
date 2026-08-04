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
            var monitor = ExecutionMonitor.For(context);

            monitor?.CheckExecutionTime(this);

            object? methodObject = MethodExpression.Evaluate(context).Value;

            ValueExpression[] parameters = EvaluateExpressionArray(Parameters, context);
            Type?[] parameterTypes = parameters.ConvertAll(expr => expr.Type?.RealType());
            object?[] parameterValues = parameters.ConvertAll(expr => expr.Value);

			switch (methodObject)
			{
			    case MethodDefinition methodDefinition:
			        return Exp.Value(methodDefinition.Invoke(parameterTypes!, parameterValues!, out var returnType), returnType);

                case ConstructorInfo[] constructors:
                {
                    MethodBase method = Type.DefaultBinder!.SelectMethod(BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance, constructors, parameterTypes, null);

                    if (method == null)
                        throw new ExpressionEvaluationException("No match found for constructor " + constructors[0].Name, this);

                    object value;

                    if (method is ConstructorInfo constructorInfo)
                        value = constructorInfo.Invoke(parameterValues);
                    else
                        throw new ExpressionEvaluationException($"{method.Name} is not a constructor", this);

                    return Exp.Value(value, method.DeclaringType);
                }

                case Delegate[] delegates:
			    {
			        MethodBase[] methods = delegates.ConvertAll<Delegate, MethodBase>(d => d.GetMethodInfo());

                    MethodBase method = Type.DefaultBinder!.SelectMethod(BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance, methods, parameterTypes!, null);

			        if (method == null)
			            throw new ExpressionEvaluationException("No match found for delegate " + MethodExpression, this);

			        object? value = method.Invoke(delegates[Array.IndexOf(methods, method)].Target, parameterValues);

			        return Exp.Value(value, ((MethodInfo)method).ReturnType);
			    }

			    case Delegate method:
			    {
			        MethodInfo methodInfo = method.GetMethodInfo();

			        object? value = methodInfo.Invoke(method.Target, parameterValues);

			        return Exp.Value(value, methodInfo.ReturnType);
			    }

			    case FunctionDefinitionExpression func:
			    {
			        var functionContext = context.CreateLocal();

			        for (int i = 0; i < parameterValues.Length; i++)
			        {
			            functionContext.Set(func.ParameterNames[i], parameterValues[i]);
			        }

			        if (monitor == null)
			            return CallResult(func.Body.EvaluateStatement(functionContext));

			        // Calling a script function is the only way a script can recurse.
			        monitor.EnterCall(this);

			        try
			        {
			            return CallResult(func.Body.EvaluateStatement(functionContext));
			        }
			        finally
			        {
			            monitor.ExitCall();
			        }
			    }
			}

            throw new ExpressionEvaluationException(MethodExpression + " is not a function", this);
        }

        /// <summary>
        /// Turns the result of a function body into the value of the call. <c>return</c>
        /// is a signal meant for the body only: letting it escape would also abort the
        /// loop or statement sequence containing the call.
        /// </summary>
        private static ValueExpression CallResult(ValueExpression bodyResult)
        {
            return bodyResult is ReturnValueExpression ? Exp.Value(bodyResult.Value, bodyResult.Type) : bodyResult;
        }

#if DEBUG
        public override string ToString()
        {
            string?[] parameters = Parameters.ConvertAll(expr => expr.ToString());

            return $"({MethodExpression}({String.Join(",", parameters)}))";
        }
#endif
    }
}
