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
using Iridium.Convert;
using Iridium.Script.CSharp;

namespace Iridium.Script
{
    /// <summary>
    /// Drives script debugging: holds breakpoints, tracks the call stack, decides when
    /// to pause, and hands control to the host through the <see cref="Break"/> event.
    /// <para/>
    /// Attach an instance to the execution scope via
    /// <see cref="ParserContext.Debugger"/> before evaluating a script, then subscribe
    /// to <see cref="Break"/> (or override <see cref="OnBreak"/>) to drive a UI.
    /// <para/>
    /// The <see cref="Break"/> event fires synchronously on the thread running the
    /// script; execution is suspended for as long as the handler runs. A UI will
    /// typically block that thread inside the handler (marshalling variable/watch
    /// requests as needed) until the user chooses how to resume. This class is not
    /// thread-safe; drive a single script execution with a single debugger instance.
    /// </summary>
    public class ScriptDebugger : IScriptDebugger
    {
        private enum StepMode
        {
            Run,
            Into,
            Over,
            Out
        }

        private readonly Stack<DebugStackFrame> _callStack = new Stack<DebugStackFrame>();

        private StepMode _stepMode = StepMode.Run;
        private int _stepDepth;
        private bool _pauseRequested;
        private bool _suppressed;

        /// <summary>The parser used to evaluate breakpoint conditions and watch expressions.</summary>
        private readonly ExpressionParser _expressionEvaluator;

        public ScriptDebugger()
        {
            _expressionEvaluator = CSharpParser.Default;
        }

        /// <summary>
        /// Creates a debugger that uses a specific parser to evaluate breakpoint
        /// conditions and watch expressions (e.g. one configured with scripting).
        /// </summary>
        public ScriptDebugger(ExpressionParser expressionEvaluator)
        {
            _expressionEvaluator = expressionEvaluator;
        }

        /// <summary>The breakpoints this debugger will stop on.</summary>
        public BreakpointCollection Breakpoints { get; } = new();

        /// <summary>
        /// When <c>false</c>, the debugger stays out of the way entirely (no breaks, no
        /// call-stack tracking). Defaults to <c>true</c>.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Raised when execution pauses. Inspect and control resumption through the
        /// event arguments. If there is no subscriber, execution simply continues.
        /// </summary>
        public event EventHandler<ScriptDebugBreakEventArgs>? Break;

        /// <summary>
        /// Builds a snapshot of the current call stack, innermost (currently executing)
        /// frame first, as a debugger UI would list it. Only meaningful while paused.
        /// <para/>
        /// This copies the whole stack on every call, so capture the result once instead
        /// of calling it repeatedly.
        /// </summary>
        public IReadOnlyList<DebugStackFrame> GetCallStack() => _callStack.ToArray();

        /// <summary>The location of the statement currently executing, if any.</summary>
        public SourceSpan CurrentLocation => _callStack.Count > 0 ? _callStack.Peek().Location : SourceSpan.Unknown;

        /// <summary>
        /// Requests that execution pause before the next statement runs (e.g. a UI
        /// "pause"/"break all" button).
        /// </summary>
        public void Pause() => _pauseRequested = true;

        ValueExpression IScriptDebugger.Execute(Expression statement, IParserContext context, Func<IParserContext, ValueExpression> evaluate)
        {
            // While evaluating breakpoint conditions or watch expressions we must not
            // recurse into the debugger.
            if (_suppressed || !IsEnabled)
                return evaluate(context);

            _callStack.Push(new DebugStackFrame(statement.SourceSpan, context));

            try
            {
                CheckBreak(statement, context);

                return evaluate(context);
            }
            finally
            {
                _callStack.Pop();
            }
        }

        private void CheckBreak(Expression statement, IParserContext context)
        {
            SourceSpan location = statement.SourceSpan;

            ScriptBreakReason reason;
            Breakpoint? breakpoint = null;

            if (_pauseRequested)
            {
                reason = ScriptBreakReason.Pause;
            }
            else if (Breakpoints.TryGet(location.Start.Line, out breakpoint) && breakpoint.Enabled && IsConditionMet(breakpoint, context))
            {
                reason = ScriptBreakReason.Breakpoint;
            }
            else if (ShouldBreakForStep())
            {
                reason = ScriptBreakReason.Step;
            }
            else
            {
                return;
            }

            _pauseRequested = false;

            if (reason == ScriptBreakReason.Breakpoint)
                breakpoint!.HitCount++;

            var args = new ScriptDebugBreakEventArgs(this, location, reason, breakpoint, context, GetCallStack());

            OnBreak(args);

            ApplyResume(args.ResumeAction);
        }

        private bool ShouldBreakForStep()
        {
            switch (_stepMode)
            {
                case StepMode.Into: return true;
                case StepMode.Over: return _callStack.Count <= _stepDepth;
                case StepMode.Out: return _callStack.Count < _stepDepth;
                default: return false;
            }
        }

        private void ApplyResume(DebugResumeAction action)
        {
            switch (action)
            {
                case DebugResumeAction.Continue:
                    _stepMode = StepMode.Run;
                    break;
                case DebugResumeAction.StepInto:
                    _stepMode = StepMode.Into;
                    break;
                case DebugResumeAction.StepOver:
                    _stepMode = StepMode.Over;
                    _stepDepth = _callStack.Count;
                    break;
                case DebugResumeAction.StepOut:
                    _stepMode = StepMode.Out;
                    _stepDepth = _callStack.Count;
                    break;
                case DebugResumeAction.Stop:
                    _stepMode = StepMode.Run;
                    throw new ScriptTerminatedException();
            }
        }

        /// <summary>
        /// Raises the <see cref="Break"/> event. Override to implement custom break
        /// handling (e.g. blocking the evaluation thread until a UI responds).
        /// </summary>
        protected virtual void OnBreak(ScriptDebugBreakEventArgs e)
        {
            Break?.Invoke(this, e);
        }

        private bool IsConditionMet(Breakpoint breakpoint, IParserContext context)
        {
            if (string.IsNullOrEmpty(breakpoint.Condition))
                return true;

            try
            {
                return context.ToBoolean(EvaluateExpression(breakpoint.Condition, context));
            }
            catch
            {
                // A broken condition shouldn't silently disable the breakpoint; err on
                // the side of stopping so the developer notices.
                return true;
            }
        }

        internal object EvaluateExpression(string expression, IParserContext context)
        {
            bool previous = _suppressed;
            _suppressed = true;

            try
            {
                return _expressionEvaluator.EvaluateToObject(expression, context);
            }
            finally
            {
                _suppressed = previous;
            }
        }

        internal T EvaluateExpression<T>(string expression, IParserContext context)
        {
            return EvaluateExpression(expression, context).Convert<T>();
        }

        internal IEnumerable<DebugVariable> GetVariablesInScope(IParserContext context)
        {
            if (context is ParserContext parserContext)
            {
                foreach (var variable in parserContext.GetVariablesInScope())
                    yield return new DebugVariable(variable.Key, variable.Value.Value, variable.Value.Type);
            }
        }
    }
}
