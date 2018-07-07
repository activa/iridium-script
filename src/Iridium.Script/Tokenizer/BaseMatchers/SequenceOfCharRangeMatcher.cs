#region License
//=============================================================================
// Iridium-Core - Portable .NET Productivity Library 
//
// Copyright (c) 2008-2017 Philippe Leybaert
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
    public class SequenceOfCharRangeMatcher : ITokenMatcher, ITokenProcessor
    {
        private readonly char _fromChar;
        private readonly char _toChar;
        private bool _seen;

        public SequenceOfCharRangeMatcher(char fromChar, char toChar)
        {
            _fromChar = fromChar;
            _toChar = toChar;
        }

        public ITokenProcessor CreateTokenProcessor()
        {
            return new SequenceOfCharRangeMatcher(_fromChar, _toChar);
        }

        public void ResetState()
        {
            _seen = false;
        }

        public TokenizerState ProcessChar(char c, string fullExpression, int currentIndex)
        {
            if (c < _fromChar || c > _toChar)
            {
                if (_seen)
                    return TokenizerState.Success;
                else
                    return TokenizerState.Fail;
            }

            _seen = true;

            return TokenizerState.Valid;
        }

        public string TranslateToken(string originalToken, ITokenProcessor tokenProcessor)
        {
            return originalToken;
        }
    }
}