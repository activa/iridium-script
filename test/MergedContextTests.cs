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
using NUnit.Framework;


namespace Iridium.Script.Test
{
    [TestFixture]
    public class MergedContextTests
    {
        [Test]
        public void TestPropertyOfLinkedObject()
        {
            var obj = new {Test = "XXX"};

            ParserContext viewData = new ParserContext(obj);

            object value;
            Type type;

            Assert.AreEqual("XXX", viewData["Test"]);
            Assert.IsTrue(viewData.Get("Test", out value, out type));

            Assert.IsInstanceOf<string>(value);
            Assert.AreEqual(typeof(string), type);
            Assert.AreEqual("XXX",value);

            Assert.IsFalse(viewData.Get("Test2", out value, out type));

        }

        [Test]
        public void TestPropertyOfMultipleLinkedObject()
        {
            var obj1 = new { Test = "XXX" };
            var obj2 = new { Value = 15.5m };

            ParserContext viewData = new ParserContext(obj1);

            viewData.Merge(obj2);

            object value;
            Type type;

            Assert.AreEqual("XXX", viewData["Test"]);
            Assert.AreEqual(15.5m, viewData["Value"]);
            Assert.IsTrue(viewData.Get("Test", out value, out type));

            Assert.IsInstanceOf<string>(value);
            Assert.AreEqual(typeof(string), type);
            Assert.AreEqual("XXX", value);

            Assert.IsTrue(viewData.Get("Value", out value, out type));

            Assert.IsInstanceOf<decimal>(value);
            Assert.AreEqual(typeof(decimal), type);
            Assert.AreEqual(15.5m, value);

            Assert.IsFalse(viewData.Get("Test2", out value, out type));
            Assert.IsFalse(viewData.Get("Value2", out value, out type));

        }

        [Test]
        public void TestPropertyOfDictionaryEntry()
        {
            ParserContext viewData = new ParserContext();

            viewData["Test"] = "XXX";

            object value;
            Type type;

            Assert.AreEqual("XXX", viewData["Test"]);
            Assert.IsTrue(viewData.Get("Test", out value, out type));

            Assert.IsInstanceOf<string>(value);
            Assert.AreEqual(typeof(string), type);
            Assert.AreEqual("XXX", value);

            Assert.IsFalse(viewData.Get("Test2", out value, out type));

        }

        [Test]
        public void TestApply()
        {
            var obj1 = new { Test = "XXX" };
            var obj2 = new { Value = 15.5m };

            ParserContext viewData1 = new ParserContext(obj1);
            ParserContext viewData2 = new ParserContext(obj2);

            ParserContext viewData = new ParserContext();

            viewData.Merge(viewData1);
            viewData.Merge(viewData2);

            object value;
            Type type;


            Assert.AreEqual("XXX", viewData["Test"]);
            Assert.AreEqual(15.5m, viewData["Value"]);
            Assert.IsTrue(viewData.Get("Test", out value, out type));

            Assert.IsInstanceOf<string>(value);
            Assert.AreEqual(typeof(string), type);
            Assert.AreEqual("XXX", value);

            Assert.IsTrue(viewData.Get("Value", out value, out type));

            Assert.IsInstanceOf<decimal>(value);
            Assert.AreEqual(typeof(decimal), type);
            Assert.AreEqual(15.5m, value);

            Assert.IsFalse(viewData.Get("Test2", out value, out type));
            Assert.IsFalse(viewData.Get("Value2", out value, out type));

        }

    }
}