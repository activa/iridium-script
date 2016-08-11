namespace Iridium.Script
{
    public class ValueOrNullExpression : BinaryExpression
    {
        public ValueOrNullExpression(Expression condition, Expression value) : base(condition, value)
        {
        }

        public Expression Condition => Left;
        public Expression Value => Right;

        public override ValueExpression Evaluate(IParserContext context)
        {
            ValueExpression result = Condition.Evaluate(context);

            if (context.ToBoolean(result.Value))
                return Value.Evaluate(context);
            else
                return Exp.Value(null, typeof(object));
        }

#if DEBUG
        public override string ToString()
        {
            return $"({Condition} ?: {Value})";
        }
#endif
    }
}