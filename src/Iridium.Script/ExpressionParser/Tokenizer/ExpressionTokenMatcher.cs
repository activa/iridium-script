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

using Iridium.Script;

namespace Iridium.Script
{
    public class ExpressionTokenMatcher : ITokenMatcher
    {
        private readonly ITokenMatcher _matcher;

        public ExpressionTokenMatcher(ITokenMatcher matcher, TokenType tokenType, TokenEvaluator tokenEvaluator)
        {
            _matcher = matcher;
            TokenType = tokenType;
            Evaluator = tokenEvaluator;
        }

        public ExpressionTokenMatcher(ITokenMatcher matcher, TokenType tokenType, int precedence, OperatorAssociativity associativity, TokenEvaluator tokenEvaluator)
        {
            _matcher = matcher;
            TokenType = tokenType;
            Evaluator = tokenEvaluator;
            Precedence = precedence;
            Associativity = associativity;
        }

        public ITokenProcessor CreateTokenProcessor()
        {
            return _matcher.CreateTokenProcessor();
        }

        public string TranslateToken(string originalToken, ITokenProcessor tokenProcessor)
        {
            return _matcher.TranslateToken(originalToken, tokenProcessor);
        }

        public TokenType TokenType { get; }
        public TokenEvaluator Evaluator { get; }
        public int Precedence { get; }

        public bool IsPartial => Root != null;

        public ExpressionTokenMatcher Root { get; set; }
        public OperatorAssociativity Associativity { get; }
        public int? NumTerms { get; set; }
    }
}