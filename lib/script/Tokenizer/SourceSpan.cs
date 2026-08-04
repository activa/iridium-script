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
/// Identifies a contiguous region of source script, from a start position up to
/// (and including) an end position.
/// <para/>
/// Spans are attached to <see cref="Expression"/> nodes so the AST retains a link
/// to the source it was compiled from. This is what a future debugger will use to
/// highlight the currently executing statement, place breakpoints on a specific
/// line and evaluate variables in the correct scope.
/// </summary>
public readonly struct SourceSpan : IEquatable<SourceSpan>
{
    /// <summary>
    /// Represents an unknown/unset span (e.g. for synthesized expressions).
    /// </summary>
    public static readonly SourceSpan Unknown = default;

    /// <summary>The position of the first character of the region.</summary>
    public SourcePosition Start { get; }

    /// <summary>The position immediately after the last character of the region.</summary>
    public SourcePosition End { get; }

    public SourceSpan(SourcePosition start, SourcePosition end)
    {
        Start = start;
        End = end;
    }

    /// <summary>True when the span refers to a real region of source.</summary>
    public bool IsKnown => Start.IsKnown;

    /// <summary>One-based line number where the region begins.</summary>
    public int Line => Start.Line;

    public bool Equals(SourceSpan other) => Start.Equals(other.Start) && End.Equals(other.End);

    public override bool Equals(object obj) => obj is SourceSpan other && Equals(other);

    public override int GetHashCode() => (Start.GetHashCode() * 397) ^ End.GetHashCode();

    public static bool operator ==(SourceSpan left, SourceSpan right) => left.Equals(right);
    public static bool operator !=(SourceSpan left, SourceSpan right) => !left.Equals(right);

    public override string ToString()
    {
        if (!IsKnown)
            return "(unknown span)";

        if (End.IsKnown && End.Line != Start.Line)
            return $"line {Start.Line}, column {Start.Column} - line {End.Line}, column {End.Column}";

        return $"line {Start.Line}, column {Start.Column}";
    }
}