using System;
using System.Runtime.Remoting;
using System.Threading;
using Iridium.Script.CSharp;
using NUnit.Framework;

namespace Iridium.Script.Test
{
    [TestFixture]
    public class ParserContextTests
    {
        [Test]
        public void Types()
        {
            ParserContext ctx = new ParserContext();

            ctx.AddType("string", typeof(string));

            Assert.True(ctx.Get("string", out var value, out var type));
            Assert.That(type.Name, Is.EqualTo("TypeName"));

            ctx = new ParserContext();

            ctx["string"] = typeof(string);

            Assert.True(ctx.Get("string", out value, out type));
            Assert.That(type.Name, Is.EqualTo("TypeName"));

        }

        [Test]
        public void Functions()
        {
            bool hasRun = false;

            ParserContext ctx = new ParserContext
            {
                ["f"] = new Action(() => hasRun = true)
            };

            Assert.True(ctx.Get("f", out var value, out var type));

            Assert.False(hasRun);
            new CSharpParser().Evaluate("f()", ctx);
            Assert.True(hasRun);
        }


        [Test]
        public void GlobalLocal()
        {
            object value;
            Type type;

            var ctx = new ParserContext();

            ctx["a"] = 1;

            Assert.That(ctx.Get("a", out value, out type ), Is.True);
            Assert.That(value, Is.EqualTo(1));

            var localCtx = ctx.CreateLocal();

            localCtx.SetLocal("a",2);

            Assert.That(ctx.Get("a", out value, out type), Is.True);
            Assert.That(value, Is.EqualTo(1));
            Assert.That(localCtx.Get("a", out value, out type), Is.True);
            Assert.That(value, Is.EqualTo(2));



        }


    }
}