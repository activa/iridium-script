//=============================================================================
// VeloxDB Core - Portable .NET Productivity Library 
//
// Copyright (c) 2008-2015 Philippe Leybaert
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

using System.Collections;

namespace Iridium.Script
{
    public class ForEachExpression : Expression
    {
        public Expression Expression { get; set; }
        public VariableExpression Iterator { get; set; }
        public Expression Body { get; set; }

        public override ValueExpression Evaluate(IParserContext context)
        {
            IEnumerable collection = Expression.Evaluate(context).Value as IEnumerable;

            if (collection != null)
                foreach (var item in collection)
                {
                    var localContext = context.CreateLocal();

                    localContext.Set(Iterator.VarName, item);

                    var returnValue = Body.Evaluate(localContext);

                    if (returnValue is ReturnValueExpression || returnValue is BreakLoopExpression)
                    {
                        return returnValue;
                    }
                }

            return Exp.NoValue();
        }
    }
}
