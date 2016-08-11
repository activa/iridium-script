#region License
//=============================================================================
// VeloxDB Core - Portable .NET Productivity Library 
//
// Copyright (c) 2008-2015 Philippe Leybaert
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

namespace Iridium.Script
{
    public class CompositeMatcher : ITokenMatcher
    {
        public CompositeMatcher(params ITokenMatcher[] tokens)
        {
            TokenMatchers = tokens;
        }

        public ITokenProcessor CreateTokenProcessor()
        {
            ITokenProcessor[] tokenProcessors = new ITokenProcessor[TokenMatchers.Length];

            for (int i=0;i<TokenMatchers.Length;i++)
                tokenProcessors[i] = TokenMatchers[i].CreateTokenProcessor();

            return new CompositeTokenProcessor(tokenProcessors);
        }

        protected ITokenMatcher[] TokenMatchers { get; }

        protected sealed class CompositeTokenProcessor : ITokenProcessor
        {
            private int _firstIndex;
            private int _current;

            public ITokenProcessor[] TokenProcessors { get; }
            public int[] StartIndexes { get; }

            public CompositeTokenProcessor(ITokenProcessor[] tokens)
            {
                TokenProcessors = tokens;
                StartIndexes = new int[tokens.Length];
            }

            public void ResetState()
            {
                TokenProcessors[0].ResetState();
                _current = 0;
                _firstIndex = -1;
                StartIndexes[0] = 0;
            }

            public TokenizerState ProcessChar(char c, string fullExpression, int currentIndex)
            {
                TokenizerState state = TokenProcessors[_current].ProcessChar(c, fullExpression, currentIndex);

                if (state == TokenizerState.Success)
                {
                    _current++;

                    if (_current == TokenProcessors.Length)
                        return TokenizerState.Success;

                    StartIndexes[_current] = currentIndex - _firstIndex;
                    TokenProcessors[_current].ResetState();

                    state = TokenProcessors[_current].ProcessChar(c, fullExpression, currentIndex);
                }

                if (state == TokenizerState.Fail)
                    return TokenizerState.Fail;

                if (_current == 0 && _firstIndex < 0)
                    _firstIndex = currentIndex;

                return TokenizerState.Valid;
            }

        }

        string ITokenMatcher.TranslateToken(string originalToken, ITokenProcessor tokenProcessor)
        {
            return TranslateToken(originalToken, (CompositeTokenProcessor) tokenProcessor);
        }

        protected virtual string TranslateToken(string originalToken, CompositeTokenProcessor tokenProcessor)
        {
            return originalToken;
        }

    }
}