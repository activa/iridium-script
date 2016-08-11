namespace Iridium.Script
{
    public class WhileExpression : Expression
    {
        public Expression ConditionExpression { get; set; }
        public Expression Body { get; set; }

        public override ValueExpression Evaluate(IParserContext context)
        {
            while (true)
            {
                bool loop = context.ToBoolean(ConditionExpression.Evaluate(context).Value);

                if (!loop)
                    break;

                var localContext = context.CreateLocal();

                var returnValue = Body.Evaluate(localContext);

                if (returnValue is ReturnValueExpression)
                    return returnValue;

                if (returnValue is BreakLoopExpression)
                    break;

            }

            return Exp.NoValue();
        }
    }
}