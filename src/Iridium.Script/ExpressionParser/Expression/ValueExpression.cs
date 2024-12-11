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

namespace Iridium.Script
{
    public class ValueExpression : Expression , IValueWithType
    {
        public Type Type { get; }
        public object Value { get; }

        public ValueExpression(object value, Type type)
        {
            Value = value;
            Type = type;

            if (Type == typeof(object) && Value != null)
                Type = Value.GetType();
        }

        public override ValueExpression Evaluate(IParserContext context) => this;

        public override string ToString() => Value.ToString();
    }

    public class ValueExpression<T> : ValueExpression
    {
        public ValueExpression(T value) : base(value, typeof(T))
        {
        }

        public new T Value => (T)base.Value;
    }

    public class NoValueExpression : ValueExpression
    {
        public NoValueExpression() : base(null, null)
        {
        }
    }
}