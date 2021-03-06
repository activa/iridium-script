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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Iridium.Script.CSharp;
using NUnit.Framework;

namespace Iridium.Script.Test
{
    [TestFixture]
    public class CSharpParserFixture
    {
        private class DataClass
        {
            public DataClass()
            {
            }

            public DataClass(int int1)
            {
                Int1 = int1;
            }


            public DataClass(string string1, int int1)
            {
                String1 = string1;
                Int1 = int1;
            }

            public string String1;
            public int Int1;

            public int Method0() { return 2; }
            public int Method1(int x) { return x * 2; }
            public int Method2(int x, int y) { return x + y; }

            public static int Static1 = 500;
            public static int Static2 => 501;

            public int this[int i] => i * 2;
            public int this[int x, int y] => x + y;
        }
        
        private ExpressionParser CreateParserWithContext()
        {
            var parser = new CSharpParser();
            var context = CreateTestContext();

            parser.DefaultContext = context;

            return parser;
        }
        
        private ParserContext CreateTestContext()
        {
            DataClass dataObject = new DataClass
            {
                String1 = "blabla1",
                Int1 = 123
            };

            var context = new ParserContext();

            context.AddType("Math", typeof(Math));
            context.Set("Data", dataObject);
            context.AddType("DataClass", typeof(DataClass));
            context.Set("Func", new Converter<int, int>(Func));
            context.AddFunction("Max", typeof(Math), "Max");
            context.AddFunction("fmt", typeof(String), "Format");
            context.Set("Value10", 10, typeof(int));
            context.Set("NullableValue5", 5, typeof(int?));
            context.Set("NullableValueNull", null, typeof(int?));
            context.Set("MyArray", new int[] { 1, 2, 4, 8, 16 });
            context.Set("MyCollection", new List<int>(new int[] { 1, 2, 4, 8, 16 }));
            context.Set("MyArray2", new int[,] { { 1, 2 }, { 2, 4 }, { 4, 8 }, { 8, 16 }, { 16, 32 } });

            context.Set("f", new Func<int,int>(i => i*2));

            return context;
        }

        private static int Func(int i)
        {
            return i * 5;
        }

        [Test]
        public void ComplexExpressions()
        {
            var parser = CreateParserWithContext();

            Assert.AreEqual(435, parser.Evaluate<int>("Math.Max(Data.Method2(Data.Int1+10,300),Data.Method1(Data.Int1))+(\"x\" + 5).Length"));
            Assert.AreEqual(17, parser.Evaluate<int>("Data.Method2(Data.Method2(3,4),Data.Method1(5))"));
            Assert.AreEqual(100, parser.Evaluate<int>("Max(Max(100,5),Func(10))"));
            Assert.AreEqual(1000, parser.Evaluate<int>("Max(Max(100,5),Func(200))"));
        }

        [Test]
        public void DefaultValueExpression()
        {
            var context = new FlexContext
            {
                {"a", ""}, 
                {"b", "z"}
            };

            Assert.AreEqual("x", new CSharpParser().Evaluate<string>("a ?: \"x\"",context));
            Assert.AreEqual("z", new CSharpParser().Evaluate<string>("b ?: \"x\"", context));
        }

        [Test]
        public void StringExpressions()
        {
            Assert.AreEqual("ab", new CSharpParser().Evaluate<string>("string.Concat(\"a\",\"b\")"));
            Assert.AreEqual("ab", new CSharpParser().Evaluate<string>("\"a\" + \"b\")"));
        }

        [TestCase("a", "a", System.StringComparison.InvariantCulture, true)]
        [TestCase("a", "b", System.StringComparison.InvariantCulture, false)]
        [TestCase("a", "A", System.StringComparison.InvariantCulture, false)]
        [TestCase("a", "a", System.StringComparison.InvariantCultureIgnoreCase, true)]
        [TestCase("a", "b", System.StringComparison.InvariantCultureIgnoreCase, false)]
        [TestCase("a", "A", System.StringComparison.InvariantCultureIgnoreCase, true)]
        public void StringComparison(string s1, string s2, StringComparison stringComparison, bool equal)
        {
            var context = new ParserContext
            {
                StringComparison = stringComparison
            };

            var parser = new CSharpParser();

            Assert.That(parser.Evaluate<bool>($"\"{s1}\" == \"{s2}\"",context), Is.EqualTo(equal));
            Assert.That(parser.Evaluate<bool>($"\"{s1}\" != \"{s2}\"",context), Is.Not.EqualTo(equal));
        }

        [Test]
        public void MemberMethods()
        {
            var parser = CreateParserWithContext();

            Assert.AreEqual(2, parser.Evaluate<int>("Data.Method0()"));
            Assert.AreEqual(2, parser.Evaluate<int>("Math.Max(1,2)"));
            Assert.AreEqual(21, parser.Evaluate<int>("Data.Method0() + Data.Method1(5) + Data.Method2(5,4)"));
        }

        [TestCase("'x'", ExpectedResult = 'x')]
        [TestCase("'\\a'", ExpectedResult = '\a')]
        [TestCase("'\\b'", ExpectedResult = '\b')]
        [TestCase("'\\n'", ExpectedResult = '\n')]
        [TestCase("'\\r'", ExpectedResult = '\r')]
        [TestCase("'\\f'", ExpectedResult = '\f')]
        [TestCase("'\\t'", ExpectedResult = '\t')]
        [TestCase("'\\v'", ExpectedResult = '\v')]
        [TestCase("'\\0'", ExpectedResult = '\0')]
        [TestCase("'\\''", ExpectedResult = '\'')]
        [TestCase("'\\\"'", ExpectedResult = '"')]
        [TestCase("'\"'", ExpectedResult = '"')]
        [TestCase("'\\\\'", ExpectedResult = '\\')]
        [TestCase("'\\x45'", ExpectedResult = '\x45')]
        [TestCase("'\\x4545'", ExpectedResult = '\x4545')]
        public char CharLiterals(string expr)
        {
            return new CSharpParser().Evaluate<char>(expr);
        }

        [TestCase("123", ExpectedResult = 123)]
        [TestCase("123L", ExpectedResult = 123L)]
        [TestCase("123l", ExpectedResult = 123l)]
        [TestCase("123u", ExpectedResult = 123u)]
        [TestCase("123U", ExpectedResult = 123U)]
        [TestCase("123LU", ExpectedResult = 123LU)]
        [TestCase("123lU", ExpectedResult = 123lU)]
        [TestCase("123Lu", ExpectedResult = 123Lu)]
        [TestCase("123lu", ExpectedResult = 123lu)]
        public object NumericLiterals(string expr)
        {
            return new CSharpParser().EvaluateToObject(expr);
        }
        

        [Test]
        public void StringLiterals()
        {
            var parser = new CSharpParser();
            
            Assert.AreEqual("xyz", parser.Evaluate<string>("\"xyz\""));
            Assert.AreEqual("\n", parser.Evaluate<string>(@"""\n"""));
            Assert.AreEqual("\f", parser.Evaluate<string>(@"""\f"""));
            Assert.AreEqual("\"", parser.Evaluate<string>(@"""\"""""));
            Assert.AreEqual("\x45r\n", parser.Evaluate<string>(@"""\x45r\n"""));
            Assert.AreEqual("\x45b\n", parser.Evaluate<string>(@"""\x45b\n"""));
            Assert.AreEqual("\x45bf\n", parser.Evaluate<string>(@"""\x45bf\n"""));
            Assert.AreEqual("\x45bff\n", parser.Evaluate<string>(@"""\x45bff\n"""));
        }


        [Test]
        public void ObjectCreation()
        {
            var parser = CreateParserWithContext();

            Assert.IsInstanceOf<DataClass>(parser.Evaluate<object>("new DataClass(5)"));

            Assert.AreEqual(5, parser.Evaluate<int>("(new DataClass(5)).Int1"));
            Assert.AreEqual(5, parser.Evaluate<int>("new DataClass(5).Int1"));
            Assert.AreEqual(5, parser.Evaluate<int>("Math.Max(new DataClass(3+2).Int1,3)"));
        }

        [Test]
        public void Delegates()
        {
            var parser = CreateParserWithContext();

            Assert.AreEqual(10, parser.Evaluate<int>("Func(2)"));
            Assert.AreEqual(5, parser.Evaluate<int>("Max(4,5)"));
        }

        [TestCase("typeof(int)", ExpectedResult = typeof(int))]
        [TestCase("typeof(long)", ExpectedResult = typeof(long))]
        [TestCase("typeof(string)", ExpectedResult = typeof(string))]
        [TestCase("typeof(double)", ExpectedResult = typeof(double))]
        [TestCase("typeof(float)", ExpectedResult = typeof(float))]
        [TestCase("typeof(decimal)", ExpectedResult = typeof(decimal))]
        [TestCase("typeof(ulong)", ExpectedResult = typeof(ulong))]
        [TestCase("typeof(short)", ExpectedResult = typeof(short))]
        [TestCase("typeof(ushort)", ExpectedResult = typeof(ushort))]
        [TestCase("typeof(byte)", ExpectedResult = typeof(byte))]
        [TestCase("typeof(sbyte)", ExpectedResult = typeof(sbyte))]
        [TestCase("typeof(bool)", ExpectedResult = typeof(bool))]
        public Type Typeof(string expr)
        {
            return new CSharpParser().Evaluate<Type>(expr);
        }

        [TestCase("(int)5L", 5)]
        [TestCase("(long)5", 5L)]
        [TestCase("(byte)5", (byte)5)]
        public void TypeCast(string s, object value)
        {
            Assert.That(new CSharpParser().EvaluateToObject(s), Is.TypeOf(value.GetType()).And.EqualTo(value));
        }

        [TestCase("5-4*2", ExpectedResult = 5 - 4 * 2)]
        [TestCase("5*4/2", ExpectedResult = 5 * 4 / 2)]
        [TestCase("5+4/2", ExpectedResult = 5 + 4 / 2)]
        [TestCase("(5-4)*2", ExpectedResult = (5 - 4) * 2)]
        [TestCase("5*(4/2)", ExpectedResult = 5 * (4 / 2))]
        [TestCase("(5+4)/2", ExpectedResult = (5 + 4) / 2)]
        public int OperatorPrecedence(string expr)
        {
            return new CSharpParser().Evaluate<int>(expr);
        }

        [Test]
        public void UnaryNot()
        {
            var parser = new CSharpParser();

            Assert.AreEqual(false, parser.Evaluate<bool>("!(1==1)"));
            Assert.AreEqual(true, parser.Evaluate<bool>("!false"));
            Assert.AreEqual(true, parser.Evaluate<bool>("!!true"));
        }

        [Test]
        public void UnaryMinus()
        {
            var parser = new CSharpParser();

            Assert.AreEqual(-2, parser.Evaluate<int>("-2"));
            Assert.AreEqual(3, parser.Evaluate<int>("5+-2"));
            Assert.AreEqual(-1, parser.Evaluate<int>("-(3-2)"));
        }

        [Test]
        public void BitwiseComplement()
        {
            var parser = new CSharpParser();

            Assert.AreEqual(~2, parser.Evaluate<int>("~2"));
            Assert.AreEqual(5 + ~2, parser.Evaluate<int>("5+~2"));
            Assert.AreEqual(~(3 - 2), parser.Evaluate<int>("~(3 - 2)"));
        }

        [Test]
        public void StaticFields()
        {
            var parser = CreateParserWithContext();

            Assert.AreEqual(500, parser.Evaluate<int>("DataClass.Static1"));
            Assert.AreEqual(501, parser.Evaluate<int>("DataClass.Static2"));
        }

        [Test]
        public void NullableLifting()
        {
            var parser = CreateParserWithContext();

            Assert.AreEqual(15, parser.Evaluate<int?>("Value10 + NullableValue5"));
            Assert.IsInstanceOf<int>(parser.Evaluate<int?>("Value10 + NullableValue5"));
            Assert.AreEqual(null, parser.Evaluate<int?>("Value10 + NullableValueNull"));

        }

        [Test]
        public void Indexing()
        {
            var parser = CreateParserWithContext();

            Assert.AreEqual(30, parser.Evaluate<int>("Data[Func(5),5]"));
        }

        [Test]
        public void ArrayIndexing()
        {
            var parser = CreateParserWithContext();

            Assert.AreEqual(8, parser.Evaluate<int>("MyArray[3]"));
            Assert.AreEqual(8, parser.Evaluate<int>("MyArray2[2,1]"));

            Assert.AreEqual(16, parser.Evaluate<int>("MyArray[Data.Method0()+2]"));
            Assert.AreEqual(8, parser.Evaluate<int>("MyArray2[Data.Method0()+1,0]"));

        }

        [Test]
        public void ListIndexing()
        {
            var parser = CreateParserWithContext();

            Assert.AreEqual(8, parser.Evaluate<int>("MyCollection[3]"));

            Assert.AreEqual(16, parser.Evaluate<int>("MyCollection[Data.Method0()+2]"));
        }

        [Test]
        public void Ternary()
        {
            var parser = new CSharpParser();
            var context = new ParserContext();

            parser.DefaultContext = context;

            Assert.AreEqual(1, parser.Evaluate<int>("true ? 1:2"));
            Assert.AreEqual(2, parser.Evaluate<int>("false ? 1:2"));

            context.Set("a", 1);

            Assert.AreEqual(1, parser.Evaluate<int>("a==1 ? 1 : 2"));
            Assert.AreEqual(2, parser.Evaluate<int>("a!=1 ? 1 : 2"));
            Assert.AreEqual("1", parser.Evaluate<string>("a==1 ? \"1\" : \"2\""));
            Assert.AreEqual("2", parser.Evaluate<string>("a!=1 ? \"1\" : \"2\""));
            Assert.AreEqual(1, parser.Evaluate<int>("a==1 ? 1 : a==2 ? 2 : a==3 ? 3 : 4"));

            Assert.AreEqual("x", parser.Evaluate<string>("a==1 ? \"x\" : a==2 ? \"y\" : a==3 ? \"z\" : \"error\""));
            context.Set("a", 2);
            Assert.AreEqual("y", parser.Evaluate<string>("a==1 ? \"x\" : a==2 ? \"y\" : a==3 ? \"z\" : \"error\""));
            context.Set("a", 3);
            Assert.AreEqual("z", parser.Evaluate<string>("a==1 ? \"x\" : a==2 ? \"y\" : a==3 ? \"z\" : \"error\""));
            context.Set("a", 56443);
            Assert.AreEqual("error", parser.Evaluate<string>("a==1 ? \"x\" : a==2 ? \"y\" : a==3 ? \"z\" : \"error\""));

        }

        [Test]
        public void Comparisons()
        {
            var parser = new CSharpParser();
            var context = new ParserContext();

            Assert.IsTrue(parser.Evaluate<bool>("1==1"));
            Assert.IsTrue(parser.Evaluate<bool>("2>=1"));
            Assert.IsTrue(parser.Evaluate<bool>("2>1"));
            Assert.IsTrue(parser.Evaluate<bool>("1<2"));
            Assert.IsFalse(parser.Evaluate<bool>("2<1"));

            context.Set("NullString", null, typeof(string));
            context.Set("ShortValue", 4, typeof(short));
            context.Set("NullableValue5",5,typeof(int?));

            Assert.IsTrue(parser.Evaluate<bool>("NullString == null", context));
            Assert.IsTrue(parser.Evaluate<bool>("NullableValue5 == 5", context));
            Assert.IsTrue(parser.Evaluate<bool>("ShortValue == 4", context));
        }

        [Test]
        public void AsOperator()
        {
            var parser = new CSharpParser();

            Assert.AreEqual("x", parser.Evaluate<string>("\"x\" as string"));
            Assert.AreEqual(null, parser.Evaluate<string>("5 as string"));
        }

        [Test]
        public void IsOperator()
        {
            var parser = new CSharpParser();

            Assert.IsTrue(parser.Evaluate<bool>("\"x\" is string"));
            Assert.IsFalse(parser.Evaluate<bool>("5 is string"));
            Assert.IsFalse(parser.Evaluate<bool>("null is string"));
        }

        [Test]
        public void NullValueOperator()
        {
            var parser = CreateParserWithContext();

            Assert.AreEqual(10, parser.Evaluate<int>("NullableValueNull ?? 10"));
            Assert.AreEqual(5, parser.Evaluate<int>("NullableValue5 ?? 10"));
        }

        [Test]
        public void NullOrValueOperator()
        {
            var parser = CreateParserWithContext();

            Assert.AreEqual(123, parser.EvaluateToObject("(2>1) :: 123"));
            Assert.AreEqual(null, parser.EvaluateToObject("(1>2) :: 123"));
        }

        [Test]
        public void Assignment()
        {
            var parser = new CSharpParser();
            var context = new ParserContext();

            context.AssignmentPermissions = AssignmentPermissions.NewVariable;

            Assert.AreEqual(5, parser.Evaluate<int>("aaa = 5", context));

            Assert.IsTrue(parser.Evaluate<bool>("aaa == 5", context));

            context.AssignmentPermissions = AssignmentPermissions.ExistingVariable;

            Assert.AreEqual(100, parser.Evaluate<int>("aaa = 100", context));
            Assert.IsTrue(parser.Evaluate<bool>("aaa == 100", context));

            context.AssignmentPermissions = AssignmentPermissions.Variable;

            Assert.AreEqual(200, parser.Evaluate<int>("aaa = bbb = 100*2", context));
            Assert.IsTrue(parser.Evaluate<bool>("aaa == 200", context));
            Assert.IsTrue(parser.Evaluate<bool>("bbb == 200", context));

        }

        [Test]
        //Bin[ExpectedException(typeof(IllegalAssignmentException))]
        public void PropertyAssignmentNotAllowed()
        {
            try
            {
                var parser = CreateParserWithContext();

                parser.DefaultContext.AssignmentPermissions = AssignmentPermissions.None;

                Assert.AreEqual(123, parser.Evaluate<int>("Data.Int1 = 123"));

                Assert.Fail();
            }
            catch (IllegalAssignmentException)
            {
                
                
            }
        }


        [Test]
        public void PropertyAssignment()
        {
            var parser = CreateParserWithContext();

            parser.DefaultContext.AssignmentPermissions = AssignmentPermissions.Property;

            Assert.AreEqual(123, parser.Evaluate<int>("Data.Int1 = 123"));

            Assert.AreEqual(123,parser.Evaluate<int>("Data.Int1"));
        }

        public class XElement
        {
            public string Attribute(XName xName)
            {
                return "attr[" + xName + "]";
            }
        }

        public class XName
        {
            private string _name;

            public static implicit operator XName(string s)
            {
                XName xName = new XName();

                xName._name = s;

                return xName;
            }

            public override string ToString()
            {
                return _name;
            }
        }


        [Test]
        public void CustomImplicitConversions()
        {
            XElement xEl = new XElement();

            var parser = CreateParserWithContext();

            parser.DefaultContext.Set("xEl", xEl);

            Assert.AreEqual("attr[Test]", parser.Evaluate<string>("xEl.Attribute(\"Test\")"));
        }

        [Test]
        public void CustomOperators()
        {
            var parser = CreateParserWithContext();

            parser.DefaultContext.Set("date1", DateTime.Now);
            parser.DefaultContext.Set("date2",DateTime.Now.AddHours(1));

            Assert.IsTrue(parser.Evaluate<bool>("date1 < date2"));
            Assert.IsFalse(parser.Evaluate<bool>("date1 > date2"));
            Assert.IsFalse(parser.Evaluate<bool>("date1 == date2"));
            Assert.AreEqual(1,(int)parser.Evaluate<TimeSpan>("date2 - date1").TotalHours);

        }

        [Test]
        public void ExpressionTree()
        {
            IParserContext context = new ParserContext();

            ExpressionWithContext expr = new ExpressionWithContext(context, Exp.Add(Exp.Value(4), Exp.Value(5)));

            Assert.AreEqual(9, expr.Evaluate().Value);
            Assert.AreEqual(typeof(int), expr.Evaluate().Type);

            expr = new ExpressionWithContext(context,Exp.Add(Exp.Add(Exp.Value(4), Exp.Value((long)5)), Exp.Value(6)));

            Assert.AreEqual(15L, expr.Evaluate().Value);
            Assert.AreEqual(typeof(long), expr.Evaluate().Type);

            expr = new ExpressionWithContext(context,Exp.Op("<<", Exp.Value((long)4), Exp.Value(2)));

            Assert.AreEqual(16L, expr.Evaluate().Value);
        }

        [Test]
        public void DynObject()
        {
            var parser = new CSharpParser();

            DynamicObject dynObj = new DynamicObject();

            dynObj.Apply(new DataClass(5));

            ParserContext context = new ParserContext(dynObj);

            Assert.AreEqual(2, parser.Evaluate<int>("Method0()",context));
            Assert.AreEqual(21, parser.Evaluate<int>("Method0() + Method1(5) + Method2(Int1,4)", context));


            
        }

        private static ParserContext SetupFalsyContext(ParserContextBehavior behavior)
        {
            ParserContext context = new ParserContext(behavior);

            context.Set<object>("NullValue", null);
            context.Set<object>("RandomObject",new object());
            context.Set("EmptyString", "");
            context.Set("NonEmptyString", "x");
            context.Set("ZeroNumber",0);
            context.Set("NonZeroNumber",111);
            context.Set("EmptyList", new int[0]);
            context.Set("NonEmptyList", new int[1]);

            return context;
        }

        [Test]
        //[ExpectedException(typeof(NullReferenceException))]
        public void NotFalsyNull()
        {
            try
            {
                var parser = new CSharpParser();

                ParserContext context = SetupFalsyContext(ParserContextBehavior.Default);

                parser.Evaluate<bool>("!!NullValue", context);

                Assert.Fail();
            }
            catch(NullReferenceException)
            {
            }
        }

        [Test]
        //[ExpectedException(typeof(ArgumentException))]
        public void NotFalsyEmptyString()
        {
            try
            {
                var parser=new CSharpParser();
                
                ParserContext context = SetupFalsyContext(ParserContextBehavior.Default);

                parser.Evaluate<bool>("!!EmptyString", context);

                Assert.Fail();
            }
            catch(ArgumentException)
            {
            }
        }

        [Test]
        //[ExpectedException(typeof(ArgumentException))]
        public void NotFalsyString()
        {
            try
            {
                var parser = new CSharpParser();

                ParserContext context = SetupFalsyContext(ParserContextBehavior.Default);

                parser.Evaluate<bool>("!!NonEmptyString", context);

                Assert.Fail();
            }
            catch(ArgumentException)
            {
                
            }
        }

        [Test]
        public void FalsyEmptyString()
        {
            var parser = new CSharpParser();

            ParserContext context = SetupFalsyContext(ParserContextBehavior.EmptyStringIsFalse);

            Assert.IsFalse(parser.Evaluate<bool>("!!EmptyString",context));
        }

        [Test]
        public void FalsyString()
        {
            var parser = new CSharpParser();

            ParserContext context = SetupFalsyContext(ParserContextBehavior.NonEmptyStringIsTrue);

            Assert.IsTrue(parser.Evaluate<bool>("!!NonEmptyString", context));
        }

        [Test]
        public void FalsyNull()
        {
            var parser = new CSharpParser();

            ParserContext context = SetupFalsyContext(ParserContextBehavior.NullIsFalse);

            Assert.IsFalse(parser.Evaluate<bool>("!!NullValue", context));
        }

        [Test]
        public void FalsyNotNull()
        {
            var parser = new CSharpParser();

            ParserContext context = SetupFalsyContext(ParserContextBehavior.NotNullIsTrue);

            Assert.IsTrue(parser.Evaluate<bool>("!!RandomObject", context));
        }

        [TestCase("...", 1 + 2 + 3 + 4 + 5)]
        [TestCase(">...", 2 + 3 + 4 + 5)]
        [TestCase(">...<", 2 + 3 + 4)]
        [TestCase("...<", 1 + 2 + 3 + 4)]
        public void IntRanges(string op, int expectedResult)
        {
            var parser = new CSharpParser();

            Assert.That(parser.Evaluate<IEnumerable<int>>("1" + op + "5").Sum(n => n),Is.EqualTo(expectedResult));
        }

        [TestCase("...", 1 + 2 + 3 + 4 + 5)]
        [TestCase(">...", 2 + 3 + 4 + 5)]
        [TestCase(">...<", 2 + 3 + 4)]
        [TestCase("...<", 1 + 2 + 3 + 4)]
        public void LongRanges(string op, int expectedResult)
        {
            var parser = new CSharpParser();

            Assert.That(parser.Evaluate<IEnumerable<long>>("1L " + op + " 5L").Sum(n => n),Is.EqualTo((long)expectedResult));
        }

        private ExpressionParser CreateScriptParser(StringBuilder output)
        {
            var parser = new CScriptParser();

            ParserContext context = new ParserContext() { AssignmentPermissions = AssignmentPermissions.All };

            context.Set("print", new Action<object>(o => output.Append(o)));

            parser.DefaultContext = context;

            return parser;
        }

        [TestCase("print(1);return 5;print(2);","1", ExpectedResult = 5, TestName = "Return.Within.Sequence")]
        [TestCase("print(1);print(2);return 5;", "12", ExpectedResult = 5, TestName = "Return.After.Sequence")]
        public int ScriptWithReturnValue(string script, string expectedOutput)
        {
            var output = new StringBuilder();
            var parser = CreateScriptParser(output);

            int returnValue = parser.Evaluate<int>(script);

            Assert.That(output.ToString(),Is.EqualTo(expectedOutput));

            return returnValue;
        }

        [TestCase("foreach (x in [1...9]) print(x);", ExpectedResult = "123456789", TestName = "ForEach.Simple")]
        [TestCase("foreach (x in [1...3]) { print(x); foreach(y in [1...x]) print(y); }", ExpectedResult = "112123123", TestName = "ForEach.Nested")]
        [TestCase("foreach (x in [1...9]) { print(x); if (x>=5) break; }", ExpectedResult = "12345", TestName = "ForEach.With.Break")]
        [TestCase("x = 1; while (x<10) { print(x); x = x + 1; }", ExpectedResult = "123456789", TestName = "While.Simple")]
        [TestCase("x = 1; while (x<10) { print(x); x = x + 1; if (x > 5) break; }", ExpectedResult = "12345", TestName = "While.With.Break")]
        [TestCase("if(1==1) print(1); else print(3); print(2)",ExpectedResult = "12", TestName = "If.True.Else")]
        [TestCase("if(1==0) print(1); else print(3); print(2)", ExpectedResult = "32", TestName = "If.False.Else")]
        [TestCase("if(1==0) print(1); else if(1==1) print(3); print(2)",ExpectedResult = "32",TestName = "If.False.Else.If")]
        [TestCase("if(1==1) print(1);print(2)", ExpectedResult = "12", TestName = "If.True")]
        [TestCase("if(1==0) print(1);print(2)", ExpectedResult = "2", TestName = "If.False")]
        [TestCase("function x(a,b) { print(a); print(b); } x(1,2);", ExpectedResult = "12", TestName = "Function.Definition.Void")]
        [TestCase("function max(a,b) { return a > b ? a:b; } print(max(1,2)); print(max(5,6));", ExpectedResult = "26", TestName = "Function.Definition.WithReturnValue")]
        public string ScriptSimple(string script)
        {
            var output = new StringBuilder();
            var parser = CreateScriptParser(output);

            parser.Evaluate(script);

            return output.ToString();
        }

        [Test]
        public void IfWithReturn()
        {
            var parser = new CScriptParser();

            string output = "";

            var context = new ParserContext(ParserContextBehavior.Easy)
            {
                {"f", new Action<int>(i => output += i)}
            };


            Assert.AreEqual(5,parser.Evaluate<int>("if(1==1) { f(1); return 5; } f(2);", context));

            Assert.AreEqual("1", output);
        }

        private int[] CreateRandomArray(int n)
        {
            Random rnd = new Random();

            int[] array = new int[500];

            for (int i = 0; i < array.Length; i++)
                array[i] = rnd.Next(1000);

            return array;
        }

        [Test]
        public void ScriptSimpleSort()
        {
            int[] array = CreateRandomArray(50);

            var context = new ParserContext(ParserContextBehavior.Easy)
            {
                AssignmentPermissions = AssignmentPermissions.All
            };

            context.Add("array", array);

            string script =
                @"
foreach (i in [0...<array.Length-1])
{
   foreach (j in [i+1...<array.Length])
   {
        if (array[i] > array[j])
        {
             tmp = array[i];
             array[i] = array[j];
             array[j] = tmp;
        }
   }
}
";
            Assert.That(array,Is.Not.Ordered);

            new CScriptParser().Evaluate(script, context);

            Assert.That(array,Is.Ordered);
        }


        [Test]
        public void FunctionDefinitionSeparate()
        {
            StringBuilder output = new StringBuilder();

            var parser = CreateScriptParser(output);

            parser.Evaluate("function x(a,b) { print(a); print(b); }");

            parser.Evaluate("x(5,6)");

            Assert.That(output.ToString(), Is.EqualTo("56"));
        }

        [Test]
        public void Recursion()
        {
            var output = new StringBuilder();
            var parser = CreateScriptParser(output);

            var script = @"
function f(level)
{
   print(level);

   if (level > 1)
     f(level-1);
}

f(5);
";

            parser.Evaluate(script);

            Assert.That(output.ToString(),Is.EqualTo("54321"));
        }


        [TestCaseSource(nameof(ArithmicCases))][Parallelizable(ParallelScope.Self)]
        public void BasicBinaryArithmic(string expr, object expectedResult, CSharpParser parser)
        {
            var veloxResult = parser.EvaluateToObject(expr);

            Assert.That(veloxResult,Is.TypeOf(expectedResult.GetType()).And.EqualTo(expectedResult));

            Assert.Pass($"{expr} = {veloxResult.GetType().Name}");
        }


        public static IEnumerable<TestCaseData> ArithmicCases
        {
            get
            {
                var rand = new Random();

                var testCaseProvider = new BinaryExpressionTestCaseProvider()
                {
                    byteA = (byte) rand.Next(3, 4),
                    byteB = (byte) rand.Next(2, 3),
                    sbyteA = (sbyte) rand.Next(3, 4),
                    sbyteB = (sbyte) rand.Next(2, 3),
                    charA = (char) rand.Next(3, 4),
                    charB = (char) rand.Next(2, 3),
                    shortA = (short) rand.Next(1000, 2000),
                    shortB = (short) rand.Next(2, 3),
                    ushortA = (ushort) rand.Next(1000, 2000),
                    ushortB = (ushort) rand.Next(2, 3),
                    intA = rand.Next(1000, 2000),
                    intB = rand.Next(5, 10),
                    uintA = (uint) rand.Next(1000, 2000),
                    uintB = (uint) rand.Next(5, 10),
                    longA = rand.Next(10000, 20000),
                    longB = rand.Next(30, 40),
                    ulongA = (ulong) rand.Next(10000, 20000),
                    ulongB = (ulong) rand.Next(30, 40),
                    doubleA = rand.NextDouble()*1000 + 2000,
                    doubleB = rand.NextDouble()*10 + 30,
                    floatA = (float) rand.NextDouble()*1000 + 2000,
                    floatB = (float) rand.NextDouble()*10 + 30,
                    decA = (decimal) rand.NextDouble()*1000 + 2000,
                    decB = (decimal) rand.NextDouble()*10 + 30,
                    strA = "ABC",
                    strB = "XYZ",
                    boolA = true,
                    boolB = false
                };

                var testCases = testCaseProvider.GenerateTestCases();

                CSharpParser parser = new CSharpParser() { DefaultContext = new ParserContext(testCaseProvider)};

                foreach (var expr in testCases.Keys)
                {
                    yield return new TestCaseData(expr,testCases[expr],parser).SetName(expr);
                }
            }
        }

    }
}