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
/// Base class for the exceptions thrown when a script exceeds its
/// <see cref="ExecutionLimits"/>. Catch this to abort a runaway script without also
/// catching ordinary evaluation errors.
/// </summary>
public abstract class ScriptExecutionLimitException : ExpressionEvaluationException
{
    protected ScriptExecutionLimitException(string message, Expression expressionNode) : base(BuildMessage(message, expressionNode), expressionNode)
    {
        Position = expressionNode.SourceSpan.Start;
    }

    protected ScriptExecutionLimitException(string message, Expression expressionNode, Exception innerException) : base(BuildMessage(message, expressionNode), expressionNode, innerException)
    {
        Position = expressionNode.SourceSpan.Start;
    }

    private static string BuildMessage(string message, Expression expressionNode)
    {
        return expressionNode.SourceSpan.IsKnown ? $"{message} (at {expressionNode.SourceSpan.Start})" : message;
    }
}
