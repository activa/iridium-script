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
    public class AsExpression : BinaryExpression
    {
        public Expression ObjectExpression => Left;
        public Expression TypeExpression => Right;

        public AsExpression(Expression objectExpression, Expression typeExpression) : base(objectExpression,typeExpression)
        {
        }

        public override ValueExpression Evaluate(IParserContext context)
        {
            var className = TypeExpression.Evaluate(context).Value as TypeName;

            if (className == null)
                throw new IllegalOperandsException("as operator requires type. "  + TypeExpression + " is not a type",this);

            var checkType = className.Type;
            ValueExpression objectValue = ObjectExpression.Evaluate(context);
            Type objectType = objectValue.Type;

            if (objectValue.Value == null)
                return Exp.Value(null, checkType);

            objectType = objectType.Inspector().RealType;

            if (!objectType.Inspector().IsValueType)
                return Exp.Value(objectValue.Value, checkType);

            if ((Nullable.GetUnderlyingType(checkType) ?? checkType) == objectType)
                return Exp.Value(objectValue.Value, checkType);

            return Exp.Value(null, checkType);
        }
    }
}
