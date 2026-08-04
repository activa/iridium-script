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
using System.Linq;

namespace Iridium.Script;

public abstract class ExpressionParser(ExpressionTokenizer _tokenizer, TokenEvaluator _functionEvaluator)
{
    public TokenEvaluator FunctionEvaluator { get; } = _functionEvaluator;
    public IParserContext DefaultContext { get; set; } = new ParserContext(ParserContextBehavior.Default);

    public Expression Parse(string s)
    {
        ExpressionToken[] tokens = _tokenizer.Tokenize(s).Where(t => t.TokenType != TokenType.WhiteSpace).ToArray();

        return new ExpressionCompiler(this, tokens).Compile();
    }

    public ExpressionWithContext ParseWithContext(string s, IParserContext context)
    {
        return new ExpressionWithContext(Parse(s), context);
    }

    public ExpressionWithContext ParseWithContext(string s)
    {
        return new ExpressionWithContext(Parse(s), DefaultContext);
    }

    public object? EvaluateToObject(string s)
    {
        return ParseWithContext(s).EvaluateToObject();
    }

    public object? Evaluate(string s, out Type type)
    {
        IValueWithType value = ParseWithContext(s).Evaluate();

        type = value.Type;

        return value.Value;
    }

    public IValueWithType Evaluate(string s)
    {
        return ParseWithContext(s).Evaluate();
    }

    public T? Evaluate<T>(string s)
    {
        return ParseWithContext(s).Evaluate<T>();
    }

    public IValueWithType Evaluate(string s, IParserContext context)
    {
        return ParseWithContext(s, context).Evaluate();
    }

    public object? EvaluateToObject(string s, IParserContext context)
    {
        return ParseWithContext(s, context).EvaluateToObject();
    }

    public object? Evaluate(string s, out Type type, IParserContext context)
    {
        IValueWithType value = ParseWithContext(s, context).Evaluate();

        type = value.Type;

        return value.Value;
    }

    public T? Evaluate<T>(string s, IParserContext context)
    {
        return ParseWithContext(s, context).Evaluate<T>();
    }
}