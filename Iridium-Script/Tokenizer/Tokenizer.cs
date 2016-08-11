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

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Iridium.Script
{
    public class Tokenizer : Tokenizer<Token>
    {
        
    }

    public class Tokenizer<T> where T:Token,new()
    {
        readonly List<ITokenMatcher> _tokenMatchers = new List<ITokenMatcher>();

        private readonly bool _allowFillerTokens;

        public Tokenizer()
        {
        }

        public Tokenizer(bool allowFillerTokens)
        {
            _allowFillerTokens = allowFillerTokens;
        }

        public void AddTokenMatcher(ITokenMatcher tokenMatcher)
        {
            _tokenMatchers.Add(tokenMatcher);
        }

        private class SuccessfulMatch
        {
            public string Token;

            public int StartIndex;
            public int Length;

            public List<TokenMatcher> Matches;
        }

        public T[] Tokenize(string s)
        {
            List<T> tokens = new List<T>();

            TokenMatcher[] tokenMatchers = new TokenMatcher[_tokenMatchers.Count];

            for(int i=0;i<tokenMatchers.Length;i++)
                tokenMatchers[i] = new TokenMatcher(_tokenMatchers[i]);

            List<TokenMatcher> successfulTokens = new List<TokenMatcher>(5);
            SuccessfulMatch successMatch = null;
            
            Reset(tokenMatchers);

            int firstValidIndex = -1;
            int lastSavedIndex = -1;

            StringBuilder filler = new StringBuilder();

            for (int textIndex = 0; textIndex < s.Length; textIndex++)
            {
                char c = s[textIndex];

                bool foundToken = false;
                successfulTokens.Clear();

                //TODO: parallel processing in .NET 4.0
                foreach (var tokenMatcher in tokenMatchers)
                {
                    TokenizerState state = tokenMatcher.Feed(c, s, textIndex);

                    if (state == TokenizerState.Valid)
                        foundToken = true;
                    else if (state == TokenizerState.Success)
                        successfulTokens.Add(tokenMatcher);
                }

                if (successfulTokens.Count > 0)
                {
                    successMatch = new SuccessfulMatch
                    {
                        StartIndex = firstValidIndex, 
                        Length = textIndex - firstValidIndex, 
                        Token = successfulTokens[0].TranslateToken(s.Substring(firstValidIndex, textIndex - firstValidIndex)), 
                        Matches = new List<TokenMatcher>(successfulTokens)
                    };
                }

                if (foundToken)
                {
                    if (firstValidIndex < 0)
                        firstValidIndex = textIndex;

                    continue;
                }

                if (successMatch == null)
                {
                    if (_allowFillerTokens)
                    {
                        filler.Append(s[++lastSavedIndex]);
                        
                        textIndex = lastSavedIndex;

                        Reset(tokenMatchers);

                        firstValidIndex = -1;

                        continue;
                    }
                    else
                    {
                        string badToken;

                        if (firstValidIndex < 0)
                            badToken = s.Substring(lastSavedIndex + 1, 1);
                        else
                            badToken = s.Substring(lastSavedIndex + 1, firstValidIndex - lastSavedIndex);

                        throw new UnknownTokenException(badToken);
                    }
                }

                if (filler.Length > 0)
                {
                    T fillerToken = CreateToken(null,filler.ToString());
                    
                    tokens.Add(fillerToken);

                    filler.Length = 0;
                }

                T token = CreateToken(successMatch.Matches[0].Matcher, successMatch.Token);

                for (int i = 1; i < successMatch.Matches.Count; i++)
                {
                    T alternateToken = CreateToken(successMatch.Matches[i].Matcher, successMatch.Token);

                    token.AddAlternate(alternateToken);
                }

                tokens.Add(token);

                lastSavedIndex = textIndex - 1;

                textIndex = successMatch.StartIndex + successMatch.Length-1;

                firstValidIndex = -1;
                successMatch = null;

                Reset(tokenMatchers);
            }

            successfulTokens.Clear();

            successfulTokens.AddRange(tokenMatchers.Where(tokenMatcher => tokenMatcher.Feed('\0', s, s.Length) == TokenizerState.Success));

            if (_allowFillerTokens && filler.Length > 0)
            {
                T fillerToken = CreateToken(null, filler.ToString());

                tokens.Add(fillerToken);
            }

            if (successfulTokens.Count > 0)
            {
                string tokenText = s.Substring(firstValidIndex, s.Length - firstValidIndex);

                tokenText = successfulTokens[0].TranslateToken(tokenText);

                T token = CreateToken(successfulTokens[0].Matcher, tokenText);

                for (int i = 1; i < successfulTokens.Count; i++)
                    token.AddAlternate(CreateToken(successfulTokens[i].Matcher, tokenText));

                tokens.Add(token);
            }

            return tokens.ToArray();
        }

        private void Reset(TokenMatcher[] matchers)
        {
            foreach (var tokenMatcher in matchers)
                tokenMatcher.Reset();
        }

        public virtual T CreateToken(ITokenMatcher tokenMatcher, string token)
        {
            return new T
            {
                Text = token, 
                TokenMatcher = tokenMatcher
            };
        }

    }
}
