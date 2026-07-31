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

namespace Iridium.Script
{
    /// <summary>
    /// The hook the evaluation engine uses to give a debugger control around the
    /// execution of each statement. Implemented by <see cref="ScriptDebugger"/>.
    /// </summary>
    public interface IScriptDebugger
    {
        /// <summary>
        /// Called by the runtime immediately before a statement executes. The
        /// implementation may pause (e.g. at a breakpoint), then must invoke
        /// <paramref name="evaluate"/> to run the statement and return its value.
        /// </summary>
        /// <param name="statement">The statement about to execute (carries its <see cref="Expression.SourceSpan"/>).</param>
        /// <param name="context">The scope the statement executes in.</param>
        /// <param name="evaluate">Runs the statement; call to produce its value.</param>
        ValueExpression Execute(Expression statement, IParserContext context, Func<IParserContext, ValueExpression> evaluate);
    }
}
