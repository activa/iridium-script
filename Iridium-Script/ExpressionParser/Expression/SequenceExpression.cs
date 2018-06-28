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

using System.Linq;

namespace Iridium.Script
{
    public class SequenceExpression : Expression
    {
        public SequenceExpression(Expression[] expressions)
        {
            Expressions = expressions;
        }

        public Expression[] Expressions { get; }

        public override ValueExpression Evaluate(IParserContext context)
        {
            ValueExpression returnValue = null;

            foreach (var expression in Expressions)
            {
                returnValue = expression.Evaluate(context);

                if (returnValue is ReturnValueExpression || returnValue is BreakLoopExpression)
                    return returnValue;
            }

            if (returnValue != null)
                return returnValue;

            return Exp.Value(null, typeof (object));
        }

#if DEBUG
        public override string ToString()
        {
            return string.Join(";", Expressions.Select(e => e.ToString()).ToArray());
        }
#endif
    }
}