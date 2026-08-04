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

namespace Iridium.Script;

public static class Exp
{
    public static AddExpression Add(Expression left, Expression right) { return new AddExpression(left, right); }
    public static SubtractExpression Subtract(Expression left, Expression right) { return new SubtractExpression(left, right); }
    public static MultiplyExpression Multiply(Expression left, Expression right) => new(left, right);
    public static DivideExpression Divide(Expression left, Expression right) => new(left, right);
    public static ValueExpression<T> Value<T>(T value) { return new ValueExpression<T>(value); }
    public static ValueExpression Value(object? value, Type type) => new(value, type);
    public static ValueExpression Value(object? value) => new(value, value?.GetType() ?? typeof(object));
    public static ValueExpression Null() => new ValueExpression(null,typeof(object));
    public static ReturnValueExpression ReturnValue(object value, Type type) { return new ReturnValueExpression(value, type); }
    public static BinaryArithmicExpression Op(string op, Expression left, Expression right) { return new BinaryArithmicExpression(op, left, right); }
    public static AndAlsoExpression AndAlso(Expression left, Expression right) { return new AndAlsoExpression(left, right); }
    public static OrElseExpression OrElse(Expression left, Expression right) { return new OrElseExpression(left, right); }
    public static ValueExpression NullValue() { return Value(null, typeof(object)); }
    public static ValueExpression NoValue() { return new NoValueExpression(); }
    public static BinaryArithmicExpression Equal(Expression left, Expression right) { return new BinaryArithmicExpression("==", left, right); }
    public static FieldExpression Field(Expression target, string fieldName) { return new FieldExpression(target, fieldName); }
    public static AsExpression As(Expression target, Expression type) { return new AsExpression(target,type); }
    public static AssignmentExpression Assign(Expression left, Expression right) { return new AssignmentExpression(left,right); }
    public static BitwiseComplementExpression BitwiseComplement(Expression value) {  return new BitwiseComplementExpression(value); }
    public static CallExpression Call(Expression method, params Expression[] parameters) { return new CallExpression(method, parameters); }
    public static CoalesceExpression Coalesce(Expression value, Expression valueIfNull) { return new CoalesceExpression(value,valueIfNull);}
    public static ConditionalExpression Conditional(Expression condition, Expression trueValue, Expression falseValue) { return new ConditionalExpression(condition,trueValue,falseValue); }
    public static DefaultValueExpression DefaultValue(Expression value, Expression defaultValue) { return new DefaultValueExpression(value,defaultValue); }
}