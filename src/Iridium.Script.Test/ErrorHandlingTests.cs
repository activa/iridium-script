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

            parser.DefaultContext = new ParserContext { AssignmentPermissions = AssignmentPermissions.All };

            var result = parser.Evaluate<int>("a = 3;\nb = 4;\nreturn a * b;");

            Assert.AreEqual(12, result);
        }
    }
}
