using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Iridium.Script;

/// <summary>
/// Tracks one script execution and enforces its <see cref="ExecutionLimits"/>.
/// <para/>
/// A monitor belongs to the context the host evaluates with and is shared by all local
/// scopes created during evaluation, so a script has a single time budget and a single
/// call depth no matter how deeply it nests. It is reset at the start of every
/// top-level evaluation.
/// <para/>
/// Like the rest of the evaluation engine this is not thread-safe: evaluate a script
/// on one thread, or give each thread its own context.
/// </summary>
public sealed class ExecutionMonitor
{
    private readonly long _maxTicks;
    private readonly int _maxCallDepth;

    private long _startTimestamp;
    private int _activeScopes;
    private int _callDepth;

    internal ExecutionMonitor(ExecutionLimits limits)
    {
        Limits = limits;

        _maxTicks = limits.MaxExecutionTime is { } maxTime ? (long)(maxTime.TotalSeconds * Stopwatch.Frequency) : 0;
        _maxCallDepth = limits.MaxCallDepth ?? 0;
    }

    /// <summary>The limits being enforced.</summary>
    public ExecutionLimits Limits { get; }

    /// <summary>The number of script function calls currently on the stack.</summary>
    public int CallDepth => _callDepth;

    /// <summary>How long the current execution has been running, or zero when idle.</summary>
    public TimeSpan Elapsed => _activeScopes == 0 ? TimeSpan.Zero : TimeSpan.FromSeconds((double)(Stopwatch.GetTimestamp() - _startTimestamp) / Stopwatch.Frequency);

    internal static ExecutionMonitor? For(IParserContext context) => (context as IExecutionLimitedContext)?.ExecutionMonitor;

    /// <summary>
    /// Marks the start of a statement. The outermost scope delimits the run: it starts
    /// the clock, so that every top-level evaluation gets the full time budget.
    /// </summary>
    internal void EnterScope()
    {
        if (_activeScopes++ == 0)
            _startTimestamp = Stopwatch.GetTimestamp();
    }

    internal void ExitScope()
    {
        // Depth is also reset here so that an exception unwinding out of a run doesn't
        // leave the next one starting halfway down the stack.
        if (--_activeScopes == 0)
            _callDepth = 0;
    }

    /// <summary>
    /// Registers a script function call. Throws <see cref="ScriptStackOverflowException"/>
    /// when the script recurses too deeply, before the CLR stack actually runs out.
    /// </summary>
    internal void EnterCall(Expression node)
    {
        if (_maxCallDepth > 0 && _callDepth >= _maxCallDepth)
            throw new ScriptStackOverflowException(node, _maxCallDepth);

        // Each script call consumes an unknown number of CLR frames (nested expressions
        // evaluate recursively), so the depth limit alone can't guarantee the stack
        // holds. This is the backstop that does.
        try
        {
            RuntimeHelpers.EnsureSufficientExecutionStack();
        }
        catch (InsufficientExecutionStackException ex)
        {
            throw new ScriptStackOverflowException(node, ex);
        }

        _callDepth++;
    }

    internal void ExitCall()
    {
        _callDepth--;
    }

    /// <summary>
    /// Throws <see cref="ScriptTimeoutException"/> when the running script has used up
    /// its time budget. Called between statements, on every loop iteration and on every
    /// function call.
    /// </summary>
    internal void CheckExecutionTime(Expression node)
    {
        if (_maxTicks == 0 || _activeScopes == 0)
            return;

        if (Stopwatch.GetTimestamp() - _startTimestamp > _maxTicks)
            throw new ScriptTimeoutException(node, Limits.MaxExecutionTime!.Value);
    }
}
