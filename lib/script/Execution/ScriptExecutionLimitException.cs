using System;

namespace Iridium.Script;

/// <summary>
/// Base class for the exceptions thrown when a script exceeds its
/// <see cref="ExecutionLimits"/>. Catch this to abort a runaway script without also
/// catching ordinary evaluation errors.
/// </summary>
public abstract class ScriptExecutionLimitException : ExpressionEvaluationException
{
    protected ScriptExecutionLimitException(string message, Expression expressionNode) : base(BuildMessage(message, expressionNode), expressionNode)
    {
        Position = expressionNode.SourceSpan.Start;
    }

    protected ScriptExecutionLimitException(string message, Expression expressionNode, Exception innerException) : base(BuildMessage(message, expressionNode), expressionNode, innerException)
    {
        Position = expressionNode.SourceSpan.Start;
    }

    private static string BuildMessage(string message, Expression expressionNode)
    {
        return expressionNode.SourceSpan.IsKnown ? $"{message} (at {expressionNode.SourceSpan.Start})" : message;
    }
}
