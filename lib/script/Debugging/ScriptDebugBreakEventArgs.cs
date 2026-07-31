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

using System;
using System.Collections.Generic;

namespace Iridium.Script
{
    /// <summary>
    /// Describes a paused script and lets the host (typically an IDE) inspect state
    /// and decide how to resume. Handed to subscribers of
    /// <see cref="ScriptDebugger.Break"/> while execution is suspended on the
    /// evaluation thread.
    /// <para/>
    /// Everything on this object is meant to be used synchronously from within the
    /// break handler, before it returns and execution resumes.
    /// </summary>
    public class ScriptDebugBreakEventArgs : EventArgs
    {
        private readonly ScriptDebugger _debugger;

        internal ScriptDebugBreakEventArgs(
            ScriptDebugger debugger,
            SourceSpan location,
            ScriptBreakReason reason,
            Breakpoint? breakpoint,
            IParserContext context,
            IReadOnlyList<DebugStackFrame> callStack)
        {
            _debugger = debugger;
            Location = location;
            Reason = reason;
            Breakpoint = breakpoint;
            Context = context;
            CallStack = callStack;
            ResumeAction = DebugResumeAction.Continue;
        }

        /// <summary>The source region of the statement about to execute.</summary>
        public SourceSpan Location { get; }

        /// <summary>Why execution paused.</summary>
        public ScriptBreakReason Reason { get; }

        /// <summary>The breakpoint that was hit, or <c>null</c> when paused for another reason.</summary>
        public Breakpoint? Breakpoint { get; }

        /// <summary>The current scope. Innermost frame's context.</summary>
        public IParserContext Context { get; }

        /// <summary>The active call stack, innermost (current) frame first.</summary>
        public IReadOnlyList<DebugStackFrame> CallStack { get; }

        /// <summary>
        /// How execution should resume once the break handler returns. Defaults to
        /// <see cref="DebugResumeAction.Continue"/>. Use the helper methods for clarity.
        /// </summary>
        public DebugResumeAction ResumeAction { get; set; }

        public void Continue() => ResumeAction = DebugResumeAction.Continue;
        public void StepInto() => ResumeAction = DebugResumeAction.StepInto;
        public void StepOver() => ResumeAction = DebugResumeAction.StepOver;
        public void StepOut() => ResumeAction = DebugResumeAction.StepOut;
        public void Stop() => ResumeAction = DebugResumeAction.Stop;

        /// <summary>
        /// Evaluates an expression (e.g. a variable name or a watch expression) in the
        /// current scope. Debugging is suppressed during this evaluation so it never
        /// triggers nested breakpoints.
        /// </summary>
        public object Evaluate(string expression) => _debugger.EvaluateExpression(expression, Context);

        /// <summary>Evaluates an expression and converts the result to <typeparamref name="T"/>.</summary>
        public T Evaluate<T>(string expression) => _debugger.EvaluateExpression<T>(expression, Context);

        /// <summary>
        /// Evaluates an expression, returning <c>false</c> instead of throwing if it
        /// fails (e.g. an unknown variable). Handy for watch panels.
        /// </summary>
        public bool TryEvaluate(string expression, out object? value)
        {
            try
            {
                value = Evaluate(expression);
                return true;
            }
            catch
            {
                value = null;
                return false;
            }
        }

        /// <summary>
        /// Enumerates the variables visible in the current scope (locals shadowing
        /// outer scopes), as would populate an IDE "locals" panel.
        /// </summary>
        public IEnumerable<DebugVariable> GetVariablesInScope() => _debugger.GetVariablesInScope(Context);
    }
}
