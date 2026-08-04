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
/// Identifies a single location within a source script.
/// <para/>
/// This is the fundamental unit used both for error reporting (pointing at the
/// location where a parse error occurred) and as the building block for future
/// debugging support (mapping executing expressions back to their source location
/// for breakpoints, stepping and variable evaluation).
/// </summary>
public readonly struct SourcePosition : IEquatable<SourcePosition>
{
    /// <summary>
    /// Represents an unknown/unset position (e.g. for synthesized tokens).
    /// </summary>
    public static readonly SourcePosition Unknown = default;

    /// <summary>Zero-based character offset from the start of the script.</summary>
    public int Index { get; }

    /// <summary>One-based line number.</summary>
    public int Line { get; }

    /// <summary>One-based column number within the line.</summary>
    public int Column { get; }

    public SourcePosition(int index, int line, int column)
    {
        Index = index;
        Line = line;
        Column = column;
    }

    /// <summary>
    /// True when this position refers to a real location. Lines are one-based,
    /// so a line of zero (the struct default) indicates an unknown position.
    /// </summary>
    public bool IsKnown => Line > 0;

    public bool Equals(SourcePosition other) => Index == other.Index && Line == other.Line && Column == other.Column;

    public override bool Equals(object obj) => obj is SourcePosition other && Equals(other);

    public override int GetHashCode() => (Index * 397) ^ (Line * 17) ^ Column;

    public static bool operator ==(SourcePosition left, SourcePosition right) => left.Equals(right);
    public static bool operator !=(SourcePosition left, SourcePosition right) => !left.Equals(right);

    public override string ToString() => IsKnown ? $"line {Line}, column {Column}" : "(unknown position)";
}