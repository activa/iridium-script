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

namespace Iridium.Script
{
    public class UnaryMinusExpression(Expression value) : UnaryExpression(value)
    {
        public override ValueExpression Evaluate(IParserContext context)
        {
            ValueExpression value = Value.Evaluate(context);

            if (value.Type == typeof(decimal))
                return Exp.Value(-(decimal)value.Value);
            
            if (value.Type == typeof(double))
                return Exp.Value(-(double)value.Value);

            if (value.Type == typeof(float))
                return Exp.Value(-(float)value.Value);

            if (value.Type == typeof(uint))
                return Exp.Value(-(uint)value.Value);

            if (value.Type == typeof(int))
                return Exp.Value(-(int)value.Value);

            if (value.Type == typeof(long))
                return Exp.Value(-(long)value.Value);

            throw new IllegalOperandsException("Unary minus is not supported for " + Value, this);
        }

#if DEBUG
        public override string ToString()
        {
            return $"(-{Value})";
        }
#endif
    }
}
