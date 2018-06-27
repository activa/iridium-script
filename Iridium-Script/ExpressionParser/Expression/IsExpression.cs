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
using Iridium.Core;

namespace Iridium.Script
{
    public class IsExpression : BinaryExpression
    {
        public IsExpression(Expression objectExpression, Expression typeExpression) : base(objectExpression, typeExpression)
        {
        }

        public Expression ObjectExpression => Left;
        public Expression TypeExpression => Right;

        public override ValueExpression Evaluate(IParserContext context)
        {
            TypeName typeName = TypeExpression.Evaluate(context).Value as TypeName;
            ValueExpression objectValue = ObjectExpression.Evaluate(context);
            Type objectType = objectValue.Type;

            if (objectValue.Value == null)
                return Exp.Value(false);

            objectType = objectType.Inspector().RealType;

            if (typeName == null)
                throw new ExpressionEvaluationException("is operator requires a type. " + TypeExpression + " is not a type", this);
            
            Type checkType = typeName.Type;

            if (!objectType.Inspector().IsValueType)
                return Exp.Value(checkType.Inspector().IsAssignableFrom(objectType));

            checkType = Nullable.GetUnderlyingType(checkType) ?? checkType;

            return Exp.Value(checkType == objectType);
        }

#if DEBUG
        public override string ToString()
        {
            return $"({ObjectExpression} is {TypeExpression})";
        }
#endif
    }
}
