using System;

namespace Iridium.Script
{
    public static class Exp
    {
        public static AddExpression Add(Expression left, Expression right) { return new AddExpression(left, right); }
        public static SubtractExpression Subtract(Expression left, Expression right) { return new SubtractExpression(left, right); }
        public static MultiplyExpression Multiply(Expression left, Expression right) { return new MultiplyExpression(left, right); }
        public static DivideExpression Divide(Expression left, Expression right) { return new DivideExpression(left, right); }
        public static ValueExpression<T> Value<T>(T value) { return new ValueExpression<T>(value); }
        public static ValueExpression Value(object value, Type type) { return new ValueExpression(value, type); }
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
}