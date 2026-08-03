namespace Iridium.Script;

/// <summary>
/// Implemented by contexts that enforce <see cref="ExecutionLimits"/>. The evaluation
/// engine looks for this on the active context to find the monitor tracking the
/// current run. Keeping it separate from <see cref="IParserContext"/> avoids forcing
/// execution limits onto every context implementation.
/// </summary>
public interface IExecutionLimitedContext
{
    /// <summary>
    /// The monitor tracking the current execution, or <c>null</c> when nothing is
    /// limited. All local scopes of one execution share a single monitor.
    /// </summary>
    ExecutionMonitor? ExecutionMonitor { get; }
}
