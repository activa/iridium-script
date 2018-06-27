using System;
using System.Collections.Generic;
using System.Linq;
using Iridium.Core;
using Iridium.Script.CSharp;
using NUnit.Framework;

namespace Iridium.Script.Test
{
    [TestFixture]
    public class JsonTest
    {
        [Test]
        public void TestNamed()
        {
            string json = "{\"n\":123, \"arr\": [1,2,3], \"obj\": { \"a\": \"x\", \"b\":111}}";

            ExpressionParser parser = new CSharpParser();

            var context = new FlexContext();

            var jsonObject = JsonParser.Parse(json);

            context.Set("json", jsonObject);

            Assert.That(parser.Evaluate<int>("json.n", context), Is.EqualTo(123));
            Assert.That(parser.Evaluate<int>("json.arr[1]", context), Is.EqualTo(2));
            Assert.That(parser.Evaluate<bool>("json.n == 123", context), Is.True);
            Assert.That(parser.Evaluate<string>("json.obj.a", context), Is.EqualTo("x"));
            Assert.That(parser.Evaluate<int>("json.obj.b", context), Is.EqualTo(111));
            Assert.That(parser.Evaluate<bool>("json.obj.a == \"x\"", context), Is.True);
            Assert.That(parser.Evaluate<bool>("json.obj.a != \"x\"", context), Is.False);
            Assert.That(parser.Evaluate<bool>("json.obj.b == 111", context), Is.True);
            Assert.That(parser.Evaluate<bool>("json.obj.b != 111", context), Is.False);
        }

        [Test]
        public void TestRoot()
        {
            string json = "{\"n\":123, \"arr\": [1,2,3], \"obj\": { \"a\": \"x\", \"b\":111}}";

            ExpressionParser parser = new CSharpParser();

            var context = new FlexContext();

            
            var jsonObject = JsonParser.Parse(json);

            context.AddJsonObject(jsonObject);

            Assert.That(parser.Evaluate<int>("n", context), Is.EqualTo(123));
            Assert.That(parser.Evaluate<int>("arr[1]", context), Is.EqualTo(2));
            Assert.That(parser.Evaluate<bool>("n == 123", context), Is.True);
            Assert.That(parser.Evaluate<string>("obj.a", context), Is.EqualTo("x"));
            Assert.That(parser.Evaluate<int>("obj.b", context), Is.EqualTo(111));
            Assert.That(parser.Evaluate<bool>("obj.a == \"x\"", context), Is.True);
            Assert.That(parser.Evaluate<bool>("obj.a != \"x\"", context), Is.False);
            Assert.That(parser.Evaluate<bool>("obj.b == 111", context), Is.True);
            Assert.That(parser.Evaluate<bool>("obj.b != 111", context), Is.False);
        }


    }
}