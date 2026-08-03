using System;

namespace Iridium.Script;

/// <summary>
/// Safety limits applied to a running script, so that a faulty or hostile script
/// cannot hang the host or bring down the process with a stack overflow.
/// <para/>
/// Assign an instance to <see cref="ParserContext.ExecutionLimits"/> before evaluating.
/// New contexts start with <see cref="Default"/>.
/// </summary>
public sealed class ExecutionLimits
{
    /// <summary>The call depth allowed when no explicit limit is specified.</summary>
    public const int DefaultMaxCallDepth = 500;

    /// <summary>Limits recursion to <see cref="DefaultMaxCallDepth"/>, but not execution time.</summary>
    public static readonly ExecutionLimits Default = new();

    /// <summary>Enforces nothing.</summary>
    public static readonly ExecutionLimits None = new() { MaxCallDepth = null };

    /// <summary>
    /// The maximum wall-clock time a single evaluation may take, or <c>null</c> for
    /// no limit. When exceeded, a <see cref="ScriptTimeoutException"/> is thrown.
    /// <para/>
    /// The clock starts when the host begins an evaluation and is reset for every
    /// subsequent one, so this is a budget per run and not a total across runs.
    /// <para/>
    /// The limit is enforced between statements, on every loop iteration and on every
    /// function call, which means a single long-running .NET method called by the
    /// script still runs to completion before the script is aborted.
    /// </summary>
    public TimeSpan? MaxExecutionTime { get; init; }

    /// <summary>
    /// The maximum number of nested script function calls, or <c>null</c> for no
    /// limit. When exceeded, a <see cref="ScriptStackOverflowException"/> is thrown.
    /// <para/>
    /// This exists because an endlessly recursing script would otherwise exhaust the
    /// CLR stack, and a real <c>StackOverflowException</c> cannot be caught: it kills
    /// the process. Raising this much above the default only makes sense if the host
    /// thread runs with a correspondingly large stack.
    /// </summary>
    public int? MaxCallDepth { get; init; } = DefaultMaxCallDepth;

    internal bool IsUnlimited => MaxExecutionTime == null && MaxCallDepth is null or <= 0;

    internal ExecutionMonitor? CreateMonitor() => IsUnlimited ? null : new ExecutionMonitor(this);
}
