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

using System.Collections;
using System.Collections.Generic;

namespace Iridium.Script;

/// <summary>
/// The set of breakpoints known to a <see cref="ScriptDebugger"/>, keyed by line.
/// At most one breakpoint exists per line.
/// </summary>
public class BreakpointCollection : IEnumerable<Breakpoint>
{
    private readonly Dictionary<int, Breakpoint> _breakpointsByLine = new Dictionary<int, Breakpoint>();

    /// <summary>
    /// Adds a breakpoint on the given line (or returns the existing one).
    /// </summary>
    public Breakpoint Add(int line)
    {
        if (!_breakpointsByLine.TryGetValue(line, out var breakpoint))
        {
            breakpoint = new Breakpoint(line);
            _breakpointsByLine.Add(line, breakpoint);
        }

        return breakpoint;
    }

    /// <summary>
    /// Adds a breakpoint on the given line with a condition (or updates the
    /// condition of the existing one).
    /// </summary>
    public Breakpoint Add(int line, string condition)
    {
        var breakpoint = Add(line);

        breakpoint.Condition = condition;

        return breakpoint;
    }

    /// <summary>Adds (or replaces) a breakpoint instance.</summary>
    public void Add(Breakpoint breakpoint)
    {
        _breakpointsByLine[breakpoint.Line] = breakpoint;
    }

    /// <summary>Removes the breakpoint on the given line, if any.</summary>
    public bool Remove(int line) => _breakpointsByLine.Remove(line);

    /// <summary>
    /// Toggles a breakpoint on the given line: adds one if absent, removes it if
    /// present. Returns the added breakpoint, or <c>null</c> if it was removed.
    /// </summary>
    public Breakpoint? Toggle(int line)
    {
        if (_breakpointsByLine.Remove(line))
        {
            return null;
        }

        return Add(line);
    }

    public bool Contains(int line) => _breakpointsByLine.ContainsKey(line);

    public bool TryGet(int line, out Breakpoint breakpoint) => _breakpointsByLine.TryGetValue(line, out breakpoint!);

    /// <summary>The breakpoint on the given line, or <c>null</c> if none.</summary>
    public Breakpoint? this[int line] => _breakpointsByLine.TryGetValue(line, out var breakpoint) ? breakpoint: null;

    public void Clear() => _breakpointsByLine.Clear();

    public int Count => _breakpointsByLine.Count;

    public IEnumerator<Breakpoint> GetEnumerator() => _breakpointsByLine.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}