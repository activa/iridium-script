namespace Iridium.Script
{
    public class DefaultValueExpression : BinaryExpression
    {
        public Expression Value => Left;
        public Expression DefaultValue => Right;

        public DefaultValueExpression(Expression value, Expression defaultValue) : base(value, defaultValue)
        {
        }

        public override ValueExpression Evaluate(IParserContext context)
        {
            ValueExpression result = Value.Evaluate(context);


            if (context.ToBoolean(result.Value))
                return result;
            else
                return DefaultValue.Evaluate(context);
        }

#if DEBUG
        public override string ToString()
        {
            return $"({Value} ?: {DefaultValue})";
        }
#endif
    }
}