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

namespace Iridium.Script;

/// <summary>
/// Returned by <see cref="ITokenProcessor.ProcessChar"/> for every character fed to a
/// token processor, telling the tokenizer whether the character can still be part of
/// the token being matched.
/// </summary>
public enum TokenizerState
{
    /// <summary>
    /// The character cannot be part of this token. The processor is out of the running
    /// until the tokenizer resets it at the start of the next token.
    /// </summary>
    Fail,

    /// <summary>
    /// The character is part of the token, but the token isn't complete yet. More
    /// characters are needed before the processor can decide.
    /// </summary>
    Valid,

    /// <summary>
    /// A complete token ends just before this character. The character itself is not
    /// consumed: the tokenizer records the match and re-reads the character as the
    /// start of the next token. A processor that matches until end of input reports
    /// this when fed the terminating '\0'.
    /// </summary>
    Success
}