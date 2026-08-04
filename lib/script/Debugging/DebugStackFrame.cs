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
/// A single entry in the debugger call stack: the source region of a statement
/// that is currently executing, together with the context (scope) it runs in.
/// The innermost (currently executing) statement is the first frame.
/// </summary>
public sealed class DebugStackFrame(SourceSpan location, IParserContext context)
{
    /// <summary>The source region of the statement executing in this frame.</summary>
    public SourceSpan Location { get; } = location;

    /// <summary>
    /// The scope this frame executes in. Can be used to evaluate expressions or
    /// enumerate variables local to the frame.
    /// </summary>
    public IParserContext Context { get; } = context;

    public override string ToString() => Location.ToString();
}