#region License
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
#endregion

using System;
using System.Collections.Generic;

namespace Iridium.Script
{
    public class RangeExpression : BinaryExpression
    {
		public bool ExcludeFrom { get; }
		public bool ExcludeTo { get; }

        public Expression From => Left;
        public Expression To => Right;

        public RangeExpression(Expression from, Expression to, bool excludeFrom, bool excludeTo) : base(from, to)
        {
			ExcludeFrom = excludeFrom;
			ExcludeTo = excludeTo;
        }

        public override ValueExpression Evaluate(IParserContext context)
        {
            ValueExpression from = From.Evaluate(context);
            ValueExpression to = To.Evaluate(context);

            if (from.Type != typeof(int) && from.Type != typeof(long))
                throw new ExpressionEvaluationException("Expression " + from + " does not evaluate to int or long", from);

            if (to.Type != typeof(int) && to.Type != typeof(long))
                throw new ExpressionEvaluationException("Expression " + from + " does not evaluate to int or long", from);

            if (from.Type == typeof(long) || to.Type == typeof(long))
                return Exp.Value(Range((long)Convert.ChangeType(from.Value, typeof(long), null), (long)Convert.ChangeType(to.Value, typeof(long), null)));
            else
                return Exp.Value(Range((int)Convert.ChangeType(from.Value, typeof(int), null), (int)Convert.ChangeType(to.Value, typeof(int), null)));
        }

        private IEnumerable<int> Range(int from, int to)
        {
            if (from == to)
                yield return from;
            else if (from < to)
				for (int i = from + (ExcludeFrom ? 1:0); i <= to - (ExcludeTo ? 1:0); i++)
                    yield return i;
            else
				for (int i = from - (ExcludeFrom ? 1:0); i >= to + (ExcludeTo ? 1:0); i--)
                    yield return i;
        }

        private IEnumerable<long> Range(long from, long to)
        {
            if (from == to)
                yield return from;
			else if (from < to)
				for (long i = from + (ExcludeFrom ? 1 : 0); i <= to - (ExcludeTo ? 1 : 0); i++)
					yield return i;
			else
				for (long i = from - (ExcludeFrom ? 1 : 0); i >= to + (ExcludeTo ? 1 : 0); i--)
					yield return i;
		}

    }
}