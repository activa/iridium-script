//=============================================================================
// Iridium Script - Portable .NET Productivity Library 
//
// Tests for execution limits: runaway scripts are aborted instead of hanging
// the host or overflowing the stack.
//=============================================================================

using System;
using System.Threading;
using Iridium.Script.CSharp;
using NUnit.Framework;

namespace Iridium.Script.Test
{
    [TestFixture]
    public class ExecutionLimitsTests
    {
        private ParserContext _context;

        [SetUp]
        public void SetUp()
        {
            _context = new ParserContext { AssignmentPermissions = AssignmentPermissions.All };

            _context.Set("sleep", new Action<int>(Thread.Sleep));
        }

        private void Run(string script) => new CScriptParser().EvaluateToObject(script, _context);

        private T Run<T>(string script) => new CScriptParser().Evaluate<T>(script, _context);

        // ---------------------------------------------------------------------
        // Execution time
        // ---------------------------------------------------------------------

        [Test]
        public void ScriptWithinTimeLimitCompletes()
        {
            _context.ExecutionLimits = new ExecutionLimits { MaxExecutionTime = TimeSpan.FromSeconds(10) };

            Assert.That(Run<int>("x = 0; while (x < 100) { x = x + 1; } return x;"), Is.EqualTo(100));
        }

        [Test]
        public void InfiniteWhileLoopIsAborted()
        {
            _context.ExecutionLimits = new ExecutionLimits { MaxExecutionTime = TimeSpan.FromMilliseconds(100) };

            Assert.Throws<ScriptTimeoutException>(() => Run("x = 0; while (true) { x = x + 1; }"));
        }

        [Test]
        public void LongRunningForEachIsAborted()
        {
            _context.ExecutionLimits = new ExecutionLimits { MaxExecutionTime = TimeSpan.FromMilliseconds(100) };

            Assert.Throws<ScriptTimeoutException>(() => Run("foreach (i in [1...100000000]) { x = i; }"));
        }

        [Test]
        public void EndlessRecursionIsAbortedByTimeLimitBeforeCallDepthLimit()
        {
            _context.ExecutionLimits = new ExecutionLimits { MaxExecutionTime = TimeSpan.FromMilliseconds(100), MaxCallDepth = null };

            // Recursion that unwinds between calls never gets deep, so only the clock
            // can stop it.
            Assert.Throws<ScriptTimeoutException>(() => Run("function spin(n) { if (n > 0) spin(n - 1); else spin(1000); } spin(1000);"));
        }

        [Test]
        public void TimeLimitIsEnforcedAfterALongHostCallReturns()
        {
            _context.ExecutionLimits = new ExecutionLimits { MaxExecutionTime = TimeSpan.FromMilliseconds(50) };

            var ex = Assert.Throws<ScriptTimeoutException>(() => Run("sleep(300);\nx = 1;"))!;

            Assert.That(ex.MaxExecutionTime, Is.EqualTo(TimeSpan.FromMilliseconds(50)));
            Assert.That(ex.Position.Line, Is.EqualTo(2));
        }

        [Test]
        public void TimeBudgetAppliesPerEvaluation()
        {
            _context.ExecutionLimits = new ExecutionLimits { MaxExecutionTime = TimeSpan.FromMilliseconds(500) };

            // Five runs of ~100ms each: fine individually, over budget if the clock
            // were ever carried over from one run to the next.
            for (int i = 0; i < 5; i++)
                Assert.DoesNotThrow(() => Run("sleep(100); x = 1;"));
        }

        [Test]
        public void NoTimeLimitByDefault()
        {
            Assert.That(_context.ExecutionLimits.MaxExecutionTime, Is.Null);
            Assert.DoesNotThrow(() => Run("sleep(200); x = 1;"));
        }

        // ---------------------------------------------------------------------
        // Call depth
        // ---------------------------------------------------------------------

        [Test]
        public void EndlessRecursionThrowsInsteadOfOverflowingTheStack()
        {
            Assert.Throws<ScriptStackOverflowException>(() => Run("function f(n) { return f(n + 1); } return f(0);"));
        }

        [Test]
        public void RecursionWithinCallDepthLimitSucceeds()
        {
            _context.ExecutionLimits = new ExecutionLimits { MaxCallDepth = 20 };

            Assert.That(Run<int>("function fac(n) { if (n <= 1) return 1; return n * fac(n - 1); } return fac(10);"), Is.EqualTo(3628800));
        }

        [Test]
        public void CallDepthLimitIsReported()
        {
            _context.ExecutionLimits = new ExecutionLimits { MaxCallDepth = 10 };

            var ex = Assert.Throws<ScriptStackOverflowException>(() => Run("function f(n) { return f(n + 1); } return f(0);"))!;

            Assert.That(ex.MaxCallDepth, Is.EqualTo(10));
        }

        [Test]
        public void CallDepthIsCountedPerEvaluation()
        {
            _context.ExecutionLimits = new ExecutionLimits { MaxCallDepth = 20 };

            string script = "function fac(n) { if (n <= 1) return 1; return n * fac(n - 1); } return fac(10);";

            for (int i = 0; i < 3; i++)
                Assert.That(Run<int>(script), Is.EqualTo(3628800));
        }

        [Test]
        public void CallDepthLimitIgnoresCallsThatHaveReturned()
        {
            _context.ExecutionLimits = new ExecutionLimits { MaxCallDepth = 5 };

            // 100 sequential calls, never more than one deep.
            Assert.That(Run<int>("function inc(n) { return n + 1; } x = 0; foreach (i in [1...100]) x = inc(x); return x;"), Is.EqualTo(100));
        }

        // ---------------------------------------------------------------------
        // Configuration
        // ---------------------------------------------------------------------

        [Test]
        public void DefaultLimitsCapCallDepthOnly()
        {
            Assert.That(_context.ExecutionLimits.MaxCallDepth, Is.EqualTo(ExecutionLimits.DefaultMaxCallDepth));
            Assert.That(_context.ExecutionLimits.MaxExecutionTime, Is.Null);
            Assert.That(_context.ExecutionMonitor, Is.Not.Null);
        }

        [Test]
        public void LimitsCanBeDisabledEntirely()
        {
            _context.ExecutionLimits = ExecutionLimits.None;

            Assert.That(_context.ExecutionMonitor, Is.Null);
            Assert.That(Run<int>("x = 0; while (x < 100) { x = x + 1; } return x;"), Is.EqualTo(100));
        }

        [Test]
        public void LocalScopesShareTheOuterMonitor()
        {
            var local = (ParserContext) _context.CreateLocal();

            Assert.That(local.ExecutionMonitor, Is.SameAs(_context.ExecutionMonitor));
            Assert.That(local.ExecutionLimits, Is.SameAs(_context.ExecutionLimits));
        }

        [Test]
        public void LimitsDoNotDisturbNormalExpressionEvaluation()
        {
            Assert.That(new CSharpParser().Evaluate<int>("2 + 3 * 4"), Is.EqualTo(14));
        }
    }
}
