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

using Iridium.Convert;

namespace Iridium.Script
{
    public abstract class Expression
    {
        /// <summary>
        /// The region of source script this expression was compiled from, or
        /// <see cref="SourceSpan.Unknown"/> when unavailable.
        /// <para/>
        /// The parser populates this for statements and control-flow constructs. It
        /// provides the mapping from the AST back to the source that a debugger needs
        /// to support breakpoints, stepping and variable evaluation.
        /// </summary>
        public SourceSpan SourceSpan { get; set; } = SourceSpan.Unknown;

        public abstract ValueExpression Evaluate(IParserContext context);

        /// <summary>
        /// When <c>true</c>, this expression is invisible to the debugger: it never
        /// triggers a break itself and does not appear on the call stack (its children
        /// are still debugged). Container nodes such as statement sequences set this so
        /// that only the actual statements are treated as stepping/breakpoint units.
        /// </summary>
        protected internal virtual bool IsDebugTransparent => false;

        /// <summary>
        /// Evaluates this expression as a statement: enforces the context's
        /// <see cref="ExecutionLimits"/> and gives an attached debugger the chance to
        /// pause before it runs. This is an internal detail of the execution engine:
        /// statement-executing nodes call it on their children instead of
        /// <see cref="Evaluate"/>. With no limits and no debugger attached (or when
        /// this node isn't a real statement) it is equivalent to <see cref="Evaluate"/>.
        /// </summary>
        internal ValueExpression EvaluateStatement(IParserContext context)
        {
            var monitor = ExecutionMonitor.For(context);

            if (monitor == null)
                return EvaluateStatementCore(context);

            // The outermost statement delimits the run, so every top-level evaluation
            // starts with a fresh time budget.
            monitor.EnterScope();

            try
            {
                monitor.CheckExecutionTime(this);

                return EvaluateStatementCore(context);
            }
            finally
            {
                monitor.ExitScope();
            }
        }

        private ValueExpression EvaluateStatementCore(IParserContext context)
        {
            if (IsDebugTransparent || !SourceSpan.IsKnown)
                return Evaluate(context);

            if (context is IDebuggableContext { Debugger: { } debugger })
                return debugger.Execute(this, context, Evaluate);

            return Evaluate(context);
        }

        internal object? EvaluateStatementToObject(IParserContext context)
        {
            return EvaluateStatement(context).Value;
        }

        internal T? EvaluateStatement<T>(IParserContext context)
        {
            return EvaluateStatement(context).Value.Convert<T>();
        }

        protected static ValueExpression[] EvaluateExpressionArray(Expression[] expressions, IParserContext context)
        {
            return expressions.ConvertAll(expr => expr.Evaluate(context));
        }

        public object? EvaluateToObject(IParserContext context)
    	{
    		return Evaluate(context).Value;
    	}

    	public T? Evaluate<T>(IParserContext context)
	    {
	        var value = Evaluate(context).Value;

	        return value.Convert<T>();
	    }
    }
}
