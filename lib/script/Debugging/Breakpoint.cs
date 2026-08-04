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
/// A breakpoint on a source line. Execution pauses when it reaches a statement
/// that starts on <see cref="Line"/>, provided the breakpoint is
/// <see cref="Enabled"/> and its optional <see cref="Condition"/> is satisfied.
/// </summary>
public class Breakpoint
{
    public Breakpoint(int line)
    {
        if (line < 1)
            throw new ArgumentOutOfRangeException(nameof(line), "Line numbers are one-based.");

        Line = line;
    }

    /// <summary>One-based source line the breakpoint is set on.</summary>
    public int Line { get; }

    /// <summary>Whether the breakpoint is currently active.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Optional boolean expression; when set, execution only pauses if it
    /// evaluates to a truthy value in the current scope. Evaluated with the same
    /// engine used for the script.
    /// </summary>
    public string? Condition { get; set; }

    /// <summary>Number of times execution has paused on this breakpoint.</summary>
    public int HitCount { get; internal set; }

    public override string ToString()
        => Condition == null ? $"Breakpoint(line {Line})" : $"Breakpoint(line {Line} when '{Condition}')";
}