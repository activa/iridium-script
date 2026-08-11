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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Iridium.Script;

public class IndexExpression(Expression target, Expression[] parameters) : Expression
{
    public Expression Target { get; } = target;
    public Expression[] Parameters { get; } = parameters;

    public override ValueExpression Evaluate(IParserContext context)
    {
        return Evaluate(context, false, null);
    }

    public ValueExpression Evaluate(IParserContext context, bool assign, object? newValue)
    {
        ValueExpression targetValue = Target.Evaluate(context);

        if (targetValue.Value == null)
        {
            if ((context ?? ParserContext.Default).Behavior.HasBehavior(ParserContextBehavior.ReturnNullWhenNullReference))
            {
                return Exp.Null();
            }

            throw new ExpressionEvaluationException("Target object is null", this);
        }

        Type targetType = targetValue.Type!;
        object targetObject = targetValue.Value!;

        ValueExpression[] parameters = EvaluateExpressionArray(Parameters, context);
        Type[] parameterTypes = parameters.ConvertAll(expr => expr!.Type)!;
        object?[] parameterValues = parameters.ConvertAll(expr => expr!.Value);

        if (targetType.IsArray)
        {
            if (targetType.GetArrayRank() != parameters.Length)
                throw new ExpressionEvaluationException("Array has a different rank. Number of arguments is incorrect", this);

            var returnType = targetType.GetElementType()!;

            foreach (var t in parameterTypes)
            {
                if (t != typeof(long) && t != typeof(long?) && t != typeof(int) && t != typeof(int?) && t != typeof(short) && t != typeof(short?) && t != typeof(ushort) && t != typeof(ushort?))
                    throw new BadArgumentException(t.Name + " is not a valid type for array indexers", this);
            }

            int[] indexes = new int[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
                indexes[i] = System.Convert.ToInt32(parameterValues[i]);

            if (assign)
                ((Array)targetObject).SetValue(newValue,indexes);

            return Exp.Value(((Array)targetObject).GetValue(indexes), returnType);
        }
        else
        {
            if (targetObject is IReadOnlyList<object?> list && parameterValues.Length == 1)
            {
                var value = list[System.Convert.ToInt32(parameterValues[0])];

                return Exp.Value(value);
            }
            else
            {
                var attributes = targetType.GetCustomAttributes<DefaultMemberAttribute>(true);

                MethodInfo? methodInfo = targetType.GetMethod("get_" + attributes.First().MemberName, parameterTypes);

                if (methodInfo == null)
                    throw new ExpressionEvaluationException("No matching indexer found for " + targetType.Name, this);

                object? value = methodInfo.Invoke(targetObject, parameterValues);

                return Exp.Value(value, methodInfo.ReturnType);
            }
        }
    }

    public ValueExpression Assign(IParserContext context, object? newValue)
    {
        return Evaluate(context, true, newValue);
    }

#if DEBUG
    public override string ToString()
    {
        string[] parameters = Parameters.ConvertAll(expr => expr!.ToString())!;

        return $"({Target}[{String.Join(",", parameters)}])";
    }
#endif
}