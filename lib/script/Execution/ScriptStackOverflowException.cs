using System;

namespace Iridium.Script;

/// <summary>
/// Thrown when a script recurses deeper than <see cref="ExecutionLimits.MaxCallDepth"/>,
/// or when the CLR stack is about to run out. It replaces the fatal
/// <c>StackOverflowException</c> that runaway recursion would otherwise cause, which
/// cannot be caught and takes the whole process down.
/// </summary>
public class ScriptStackOverflowException : ScriptExecutionLimitException
{
    /// <summary>
    /// The call depth limit that was exceeded, or <c>null</c> when the script was
    /// stopped because the CLR stack ran low rather than because of the limit.
    /// </summary>
    public int? MaxCallDepth { get; }

    public ScriptStackOverflowException(Expression expressionNode, int maxCallDepth)
        : base($"Script recursed deeper than the maximum call depth of {maxCallDepth}", expressionNode)
    {
        MaxCallDepth = maxCallDepth;
    }

    public ScriptStackOverflowException(Expression expressionNode, Exception innerException)
        : base("Script recursed too deeply: not enough stack space left to continue", expressionNode, innerException)
    {
    }
}
