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
