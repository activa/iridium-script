namespace Iridium.Script
{
    public class FunctionValueExpression : ValueExpression
    {
        public FunctionValueExpression(FunctionDefinitionExpression function) : base(function, typeof(FunctionDefinitionExpression))
        {
        }
    }
}