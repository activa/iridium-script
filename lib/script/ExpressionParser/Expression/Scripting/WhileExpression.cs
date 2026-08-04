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

namespace Iridium.Script;

public class WhileExpression : Expression
{
    public required Expression ConditionExpression { get; init; }
    public required Expression Body { get; init; }

    public override ValueExpression Evaluate(IParserContext context)
    {
        var monitor = ExecutionMonitor.For(context);

        while (true)
        {
            monitor?.CheckExecutionTime(this);

            bool loop = context.ToBoolean(ConditionExpression.Evaluate(context).Value);

            if (!loop)
                break;

            var localContext = context.CreateLocal();

            var returnValue = Body.EvaluateStatement(localContext);

            if (returnValue is ReturnValueExpression)
                return returnValue;

            if (returnValue is BreakLoopExpression)
                break;

        }

        return Exp.NoValue();
    }
}