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
