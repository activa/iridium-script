//=============================================================================
// Iridium Script - Portable .NET Productivity Library 
//
// Tests for source-location tracking and location-aware error reporting.
//=============================================================================

using Iridium.Script.CSharp;
using NUnit.Framework;

namespace Iridium.Script.Test
{
    [TestFixture]
    public class ErrorHandlingTests
    {
        private static CSharpParser ExpressionParser() => new CSharpParser();
        private static CSharpParser ScriptParser() => new CScriptParser();

        // ---------------------------------------------------------------------
        // SourcePosition / SourceSpan semantics
        // ---------------------------------------------------------------------

        [Test]
        public void DefaultPositionsAreUnknown()
        {
            Assert.IsFalse(default(SourcePosition).IsKnown);
            Assert.IsFalse(SourcePosition.Unknown.IsKnown);
            Assert.IsFalse(default(SourceSpan).IsKnown);
            Assert.IsFalse(SourceSpan.Unknown.IsKnown);
        }

        [Test]
        public void KnownPositionExposesLineAndColumn()
        {
            var position = new SourcePosition(10, 3, 5);

            Assert.IsTrue(position.IsKnown);
            Assert.AreEqual(10, position.Index);
            Assert.AreEqual(3, position.Line);
            Assert.AreEqual(5, position.Column);
        }

        // ---------------------------------------------------------------------
        // Lexer errors report the offending position
        // ---------------------------------------------------------------------

        [Test]
        public void MisplacedOperatorReportsLineAndColumn()
        {
            //                                                   1 * * 2
            //                                       column ---> 1234567
            var ex = Assert.Throws<LexerException>(() => ExpressionParser().Parse("1 * * 2"));

            Assert.IsTrue(ex.Position.IsKnown);
            Assert.AreEqual(1, ex.Position.Line);
            Assert.AreEqual(5, ex.Position.Column);
        }

        [Test]
        public void ErrorMessageIncludesLineAndColumn()
        {
            var ex = Assert.Throws<LexerException>(() => ExpressionParser().Parse("1 * * 2"));

            StringAssert.Contains("line 1", ex.Message);
            StringAssert.Contains("column 5", ex.Message);
        }

        [Test]
        public void MisplacedOperatorReportsCorrectLineInMultiLineScript()
        {
            var script =
                "a = 1;\n" +      // line 1
                "b = 2 * * 3;\n" + // line 2 (error here)
                "return b;";      // line 3

            var ex = Assert.Throws<LexerException>(() => ScriptParser().Parse(script));

            Assert.AreEqual(2, ex.Position.Line);
        }

        [Test]
        public void WhileWithoutParenthesisReportsPosition()
        {
            var ex = Assert.Throws<LexerException>(() => ScriptParser().Parse("while x { }"));

            Assert.IsTrue(ex.Position.IsKnown);
            Assert.AreEqual(1, ex.Position.Line);
        }

        [Test]
        public void FunctionWithoutNameReportsPosition()
        {
            var ex = Assert.Throws<LexerException>(() => ScriptParser().Parse("function () { }"));

            Assert.IsTrue(ex.Position.IsKnown);
            Assert.AreEqual(1, ex.Position.Line);
        }

        [Test]
        public void UnexpectedCloseBraceReportsPositionOnCorrectLine()
        {
            var script =
                "a = 1;\n" + // line 1
                "}";         // line 2 (stray brace)

            var ex = Assert.Throws<LexerException>(() => ScriptParser().Parse(script));

            Assert.AreEqual(2, ex.Position.Line);
        }

        // ---------------------------------------------------------------------
        // 'break' is only meaningful inside a loop
        // ---------------------------------------------------------------------

        [Test]
        public void BreakOutsideLoopReportsPosition()
        {
            var ex = Assert.Throws<LexerException>(() => ScriptParser().Parse("x = 1;\nbreak;"));

            StringAssert.Contains("break", ex.Message);
            Assert.AreEqual(2, ex.Position.Line);
        }

        // Outside a loop body there is no position where 'break' means anything, whether
        // it is written as a statement or used as a value.
        [TestCase("break;", TestName = "Break.At.Top.Level")]
        [TestCase("if (x > 0) break;", TestName = "Break.In.If.Outside.Loop")]
        [TestCase("foreach (i in [1...3]) { print(i); }\nbreak;", TestName = "Break.After.Loop")]
        [TestCase("foreach (i in [1...3]) { function f() { break; } }", TestName = "Break.In.Function.Inside.Loop")]
        [TestCase("x = break;", TestName = "Break.As.Assigned.Value")]
        [TestCase("x = break + 1;", TestName = "Break.As.Operand")]
        [TestCase("print(break);", TestName = "Break.As.Argument")]
        [TestCase("while (break) { print(1); }", TestName = "Break.As.Loop.Condition")]
        [TestCase("foreach (i in [1...3]) { x = break; }", TestName = "Break.As.Value.Inside.Loop")]
        [TestCase("foreach (i in [1...3]) { return break; }", TestName = "Break.As.Return.Value")]
        public void BreakOutsideLoopBodyIsRejected(string script)
        {
            Assert.Throws<LexerException>(() => ScriptParser().Parse(script));
        }

        // 'return' is a statement too, so it has no value either. Unlike 'break' it is
        // valid at script level, where it produces the script's result.
        [TestCase("x = return 1;", TestName = "Return.As.Assigned.Value")]
        [TestCase("x = 1 + return 2;", TestName = "Return.As.Operand")]
        [TestCase("print(return 1);", TestName = "Return.As.Argument")]
        [TestCase("while (return 1) { print(1); }", TestName = "Return.As.Loop.Condition")]
        [TestCase("return return 1;", TestName = "Return.Of.Return")]
        [TestCase("foreach (i in [1...3]) { return break; }", TestName = "Return.Of.Break")]
        public void ReturnUsedAsValueIsRejected(string script)
        {
            var ex = Assert.Throws<LexerException>(() => ScriptParser().Parse(script));

            StringAssert.Contains("is not a value", ex.Message);
        }

        [TestCase("return 5;", ExpectedResult = 5, TestName = "Return.At.Script.Level")]
        [TestCase("a = 1; if (a > 0) { return 2; } return 3;", ExpectedResult = 2, TestName = "Return.From.Block")]
        [TestCase("foreach (i in [1...3]) { return i; }", ExpectedResult = 1, TestName = "Return.From.Loop")]
        [TestCase("function f() { return 7; } return f();", ExpectedResult = 7, TestName = "Return.From.Function")]
        public int ReturnAtScriptLevelStillProducesTheScriptValue(string script)
        {
            var context = new ParserContext { AssignmentPermissions = AssignmentPermissions.All };

            return ScriptParser().Evaluate<int>(script, context);
        }

        [TestCase("foreach (i in [1...3]) { if (i > 1) break; }", TestName = "Break.In.ForEach")]
        [TestCase("while (x < 3) { break; }", TestName = "Break.In.While")]
        [TestCase("while (x < 3) { if (x > 1) break; else break; }", TestName = "Break.In.If.Else.Inside.Loop")]
        [TestCase("foreach (i in [1...3]) break;", TestName = "Break.In.Braceless.ForEach.Body")]
        [TestCase("function f() { foreach (i in [1...3]) break; }", TestName = "Break.In.Loop.Inside.Function")]
        [TestCase("foreach (i in [1...3]) { foreach (j in [1...3]) { break; } break; }", TestName = "Break.In.Nested.Loops")]
        public void BreakInsideLoopIsAccepted(string script)
        {
            Assert.DoesNotThrow(() => ScriptParser().Parse(script));
        }

        // ---------------------------------------------------------------------
        // Tokenizer errors report the offending position
        // ---------------------------------------------------------------------

        [Test]
        public void UnknownTokenReportsLineAndColumn()
        {
            //                                                       1 + `
            //                                           column ---> 12345
            var ex = Assert.Throws<UnknownTokenException>(() => ExpressionParser().Parse("1 + `"));

            Assert.IsTrue(ex.Position.IsKnown);
            Assert.AreEqual(1, ex.Position.Line);
            Assert.AreEqual(5, ex.Position.Column);
        }

        [Test]
        public void UnknownTokenReportsCorrectLineInMultiLineScript()
        {
            var script =
                "a = 1;\n" + // line 1
                "c = `;";    // line 2 (bad backtick)

            var ex = Assert.Throws<UnknownTokenException>(() => ScriptParser().Parse(script));

            Assert.AreEqual(2, ex.Position.Line);
            StringAssert.Contains("line 2", ex.Message);
        }

        // ---------------------------------------------------------------------
        // SourceSpan is attached to the AST (foundation for future debugging)
        // ---------------------------------------------------------------------

        [Test]
        public void StatementExpressionCarriesSourceSpan()
        {
            var expression = ExpressionParser().Parse("1 + 2");

            Assert.IsTrue(expression.SourceSpan.IsKnown);
            Assert.AreEqual(1, expression.SourceSpan.Start.Line);
            Assert.AreEqual(1, expression.SourceSpan.Start.Column);
        }

        [Test]
        public void EachStatementCarriesItsOwnSourceLine()
        {
            var script =
                "a = 1;\n" +       // line 1
                "b = 2;\n" +       // line 2
                "return a + b;";   // line 3

            var sequence = (SequenceExpression) ScriptParser().Parse(script);

            Assert.AreEqual(3, sequence.Expressions.Length);
            Assert.AreEqual(1, sequence.Expressions[0].SourceSpan.Start.Line);
            Assert.AreEqual(2, sequence.Expressions[1].SourceSpan.Start.Line);
            Assert.AreEqual(3, sequence.Expressions[2].SourceSpan.Start.Line);
        }

        [Test]
        public void ControlFlowSpanCoversKeywordThroughBody()
        {
            var script =
                "if (a > 0)\n" + // line 1 (keyword)
                "  a = a + 1;";  // line 2 (body)

            var ifExpression = (IfExpression) ScriptParser().Parse(script);

            Assert.IsTrue(ifExpression.SourceSpan.IsKnown);
            Assert.AreEqual(1, ifExpression.SourceSpan.Start.Line);
            Assert.AreEqual(2, ifExpression.SourceSpan.End.Line);
        }

        [Test]
        public void TopLevelSequenceSpansWholeScript()
        {
            var script =
                "a = 1;\n" +
                "b = 2;\n" +
                "return a + b;";

            var sequence = (SequenceExpression) ScriptParser().Parse(script);

            Assert.AreEqual(1, sequence.SourceSpan.Start.Line);
            Assert.AreEqual(3, sequence.SourceSpan.End.Line);
        }

        // ---------------------------------------------------------------------
        // Location tracking does not disturb normal evaluation
        // ---------------------------------------------------------------------

        [Test]
        public void ValidScriptStillEvaluates()
        {
            var parser = ScriptParser();

            var result = parser.Evaluate<int>("a = 3;\nb = 4;\nreturn a * b;", new ParserContext { AssignmentPermissions = AssignmentPermissions.All });

            Assert.AreEqual(12, result);
        }
    }
}
