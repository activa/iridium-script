#region License
//=============================================================================
// Iridium Script - .NET scripting and templating engine 
//
// Copyright (c) 2008-2026 Philippe Leybaert
//
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal 
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in 
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.
//=============================================================================
#endregion

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

    /// <summary>
    /// Enforces nothing. This also gives up the protection against runaway recursion
    /// described on <see cref="MaxCallDepth"/>, so use it only for trusted scripts.
    /// </summary>
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
    /// <para/>
    /// A limit here is the only reliable defence. Setting it to <c>null</c> leaves nothing
    /// but the best-effort stack check in the runtime, which a runaway recursive script
    /// can outrun and bring the process down.
    /// </summary>
    public int? MaxCallDepth { get; init; } = DefaultMaxCallDepth;

    internal bool IsUnlimited => MaxExecutionTime == null && MaxCallDepth is null or <= 0;

    internal ExecutionMonitor? CreateMonitor() => IsUnlimited ? null : new ExecutionMonitor(this);
}
