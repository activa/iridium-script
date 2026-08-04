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

using System.Collections;

namespace Iridium.Script;

public class ForEachExpression : Expression
{
    public required Expression Expression { get; init; }
    public required VariableExpression Iterator { get; init; }
    public required Expression Body { get; init; }

    public override ValueExpression Evaluate(IParserContext context)
    {
        if (Expression.Evaluate(context).Value is IEnumerable enumerable)
        {
            var monitor = ExecutionMonitor.For(context);

            foreach (var item in enumerable)
            {
                monitor?.CheckExecutionTime(this);

                var localContext = context.CreateLocal();

                localContext.Set(Iterator.VarName, item);

                var returnValue = Body.EvaluateStatement(localContext);

                if (returnValue is ReturnValueExpression || returnValue is BreakLoopExpression)
                {
                    return returnValue;
                }
            }
        }

        return Exp.NoValue();
    }
}