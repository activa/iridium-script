using System;

namespace Iridium.Script;

/// <summary>
/// Thrown when a script runs longer than <see cref="ExecutionLimits.MaxExecutionTime"/>.
/// The script is aborted at the statement, loop iteration or function call where the
/// limit was noticed, which <see cref="ExpressionEvaluationException.ExpressionNode"/>
/// and <see cref="ParserException.Position"/> point at.
/// </summary>
public class ScriptTimeoutException : ScriptExecutionLimitException
{
    /// <summary>The time limit that was exceeded.</summary>
    public TimeSpan MaxExecutionTime { get; }

    public ScriptTimeoutException(Expression expressionNode, TimeSpan maxExecutionTime)
        : base($"Script execution took longer than the maximum of {maxExecutionTime.TotalMilliseconds:0}ms", expressionNode)
    {
        MaxExecutionTime = maxExecutionTime;
    }
}
