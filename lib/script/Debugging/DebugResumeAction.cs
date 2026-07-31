#region License
//=============================================================================
// Iridium Script - Portable .NET Productivity Library 
//
// Copyright (c) 2008-2018 Philippe Leybaert
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

namespace Iridium.Script
{
    /// <summary>
    /// Chosen by the host (typically a UI) while stopped at a break to control how
    /// execution should resume. Set on <see cref="ScriptDebugBreakEventArgs.ResumeAction"/>.
    /// </summary>
    public enum DebugResumeAction
    {
        /// <summary>Resume running until the next breakpoint (or completion).</summary>
        Continue,

        /// <summary>Run until the next statement, descending into nested blocks and function calls.</summary>
        StepInto,

        /// <summary>Run until the next statement at the same nesting level, skipping nested blocks and calls.</summary>
        StepOver,

        /// <summary>Run until execution returns to a shallower nesting level.</summary>
        StepOut,

        /// <summary>Abort script execution (throws <see cref="ScriptTerminatedException"/>).</summary>
        Stop
    }
}
