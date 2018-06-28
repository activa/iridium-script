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
using System.Linq;
using Iridium.Core;

namespace Iridium.Script
{
    public class ExpressionToken : Token
    {
        internal int NumTerms { get; set; }

        public new ExpressionTokenMatcher TokenMatcher => (ExpressionTokenMatcher)base.TokenMatcher;

        public ExpressionToken()
        {
            throw new NotSupportedException();
        }

        protected ExpressionToken(string token) : base(null,token)
        {
            Associativity = OperatorAssociativity.Left;
        }

        public ExpressionToken(ExpressionTokenMatcher tokenMatcher, string text) : base(tokenMatcher, text)
        {
            switch (tokenMatcher.TokenType)
            {
                case TokenType.TernaryOperator: NumTerms = 3; break;
                case TokenType.UnaryOperator: NumTerms = 1; break;
                case TokenType.Operator: NumTerms = 2; break;
            }

            if (tokenMatcher.NumTerms != null)
                NumTerms = tokenMatcher.NumTerms.Value;

            Precedence = tokenMatcher.Precedence;
            Associativity = tokenMatcher.Associativity;
            TokenType = tokenMatcher.TokenType;
            Evaluator = tokenMatcher.Evaluator;
        }

        internal TokenType TokenType { get; set; }
        internal OperatorAssociativity Associativity { get; }
        internal int Precedence { get; set; }
        internal TokenEvaluator Evaluator { get; set; }

        internal bool IsOperator => (TokenType == TokenType.Operator) || (TokenType == TokenType.UnaryOperator);
        internal bool IsTerm => (TokenType == TokenType.Term);
        internal bool IsUnary => (TokenType == TokenType.UnaryOperator);
        internal bool IsFunction => (TokenType == TokenType.FunctionCall);
        internal bool IsLeftParen => (TokenType == TokenType.LeftParen);
        internal bool IsRightParen => (TokenType == TokenType.RightParen);
        internal bool IsArgumentSeparator => TokenType == TokenType.ArgumentSeparator;
        public bool IsPartial => TokenMatcher != null && TokenMatcher.IsPartial;
        public bool IsStatementSeperator => TokenType == TokenType.StatementSeparator;

        public ExpressionToken Alternate => (ExpressionToken) Alternates?.FirstOrDefault();
        public ExpressionTokenMatcher Root => TokenMatcher.Root;
        public bool IsOpenBrace => TokenType == TokenType.OpenBrace;
        public bool IsCloseBrace => TokenType == TokenType.CloseBrace;

        public override string ToString()
        {
            return Text;
        }
    }
}