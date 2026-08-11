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
        private void Run(string script, IParserContext context) => new CScriptParser().EvaluateToObject(script, context);

        private T Run<T>(string script, IParserContext context) => new CScriptParser().Evaluate<T>(script, context);

        private ParserContext CreateContext()
        {
            var context = new ParserContext { AssignmentPermissions = AssignmentPermissions.All };

            context.Set("sleep", ((int ms) => Thread.Sleep(ms)));

            return context;
        }

        private ParserContext CreateContext(int? maxCallDepth)
        {
            var context = new ParserContext { AssignmentPermissions = AssignmentPermissions.All };

            context.ExecutionLimits = new ExecutionLimits { MaxCallDepth = maxCallDepth };

            return context;
        }

        // ---------------------------------------------------------------------
        // Execution time
        // ---------------------------------------------------------------------

        [Test]
        public void ScriptWithinTimeLimitCompletes()
        {
            var context = new ParserContext { AssignmentPermissions = AssignmentPermissions.All };
            context.ExecutionLimits = new ExecutionLimits { MaxExecutionTime = TimeSpan.FromSeconds(10) };

            Assert.That(Run<int>("x = 0; while (x < 100) { x = x + 1; } return x;", context), Is.EqualTo(100));
        }

        [Test]
        public void InfiniteWhileLoopIsAborted()
        {
            var context = new ParserContext { AssignmentPermissions = AssignmentPermissions.All };
            context.ExecutionLimits = new ExecutionLimits { MaxExecutionTime = TimeSpan.FromMilliseconds(100) };

            Assert.Throws<ScriptTimeoutException>(() => Run("x = 0; while (true) { x = x + 1; }", context));
        }

        [Test]
        public void LongRunningForEachIsAborted()
        {
            var context = new ParserContext { AssignmentPermissions = AssignmentPermissions.All };
            context.ExecutionLimits = new ExecutionLimits { MaxExecutionTime = TimeSpan.FromMilliseconds(100) };
        
            Assert.Throws<ScriptTimeoutException>(() => Run("foreach (i in [1...100000000]) { x = i; }", context));
        }

        [Test]
        public void SingleStatementScriptIsAborted()
        {
            var context = new ParserContext { AssignmentPermissions = AssignmentPermissions.All };
            context.ExecutionLimits = new ExecutionLimits { MaxExecutionTime = TimeSpan.FromMilliseconds(100) };

            // A script of one statement is not wrapped in a statement sequence, so the
            // loop itself is the root expression. The run has to be delimited by the
            // evaluation entry point, not by the first statement that happens to run.
            Assert.Throws<ScriptTimeoutException>(() => Run("while (true) { x = 1; }", context));
        }

        [Test]
        public void TimeLimitIsEnforcedByEveryEvaluationEntryPoint()
        {
            var parser = new CScriptParser();
            var context = CreateContext();

            context.ExecutionLimits = new ExecutionLimits { MaxExecutionTime = TimeSpan.FromMilliseconds(20) };

            // Only the entry points start the clock, so each overload has to do it.
            const string script = "sleep(150); x = 1;";

            Assert.Throws<ScriptTimeoutException>(() => parser.EvaluateToObject(script, context));
            Assert.Throws<ScriptTimeoutException>(() => parser.Evaluate<int>(script, context));
            Assert.Throws<ScriptTimeoutException>(() => parser.Evaluate(script, context));
            Assert.Throws<ScriptTimeoutException>(() => parser.Evaluate(script, out _, context));
        }

        [Test]
        public void EndlessRecursionIsAbortedByTimeLimitBeforeCallDepthLimit()
        {
            var context = new ParserContext { AssignmentPermissions = AssignmentPermissions.All };
            context.ExecutionLimits = new ExecutionLimits { MaxExecutionTime = TimeSpan.FromMilliseconds(100), MaxCallDepth = null };

            // Recursion that unwinds between calls never gets deep, so only the clock
            // can stop it.
            Assert.Throws<ScriptTimeoutException>(() => Run("function spin(n) { if (n > 0) spin(n - 1); else spin(1000); } spin(1000);", context));
        }

        [Test]
        public void TimeLimitIsEnforcedAfterALongHostCallReturns()
        {
            var context = CreateContext();
            context.ExecutionLimits = new ExecutionLimits { MaxExecutionTime = TimeSpan.FromMilliseconds(50) };

            var ex = Assert.Throws<ScriptTimeoutException>(() => Run("sleep(300);\nx = 1;", context))!;

            Assert.That(ex.MaxExecutionTime, Is.EqualTo(TimeSpan.FromMilliseconds(50)));
            Assert.That(ex.Position.Line, Is.EqualTo(2));
        }

        [Test]
        public void TimeBudgetAppliesPerEvaluation()
        {
            var context = CreateContext();
            context.ExecutionLimits = new ExecutionLimits { MaxExecutionTime = TimeSpan.FromMilliseconds(500) };

            // Five runs of ~100ms each: fine individually, over budget if the clock
            // were ever carried over from one run to the next.
            for (int i = 0; i < 5; i++)
                Assert.DoesNotThrow(() => Run("sleep(100); x = 1;", context));
        }

        [Test]
        public void WatchEvaluationDoesNotRestartTheTimeBudget()
        {
            var context = CreateContext();
            context.ExecutionLimits = new ExecutionLimits { MaxExecutionTime = TimeSpan.FromSeconds(30) };

            var debugger = new ScriptDebugger();

            context.Debugger = debugger;
            debugger.Breakpoints.Add(2);

            TimeSpan atBreak = TimeSpan.Zero;
            TimeSpan afterWatch = TimeSpan.Zero;

            debugger.Break += (_, e) =>
            {
                atBreak = context.ExecutionMonitor!.Elapsed;

                // A watch expression is a run nested inside the paused one: it must not
                // hand the script a fresh budget.
                e.Evaluate("1 + 1");

                afterWatch = context.ExecutionMonitor!.Elapsed;

                e.Continue();
            };

            Run("sleep(150);\nx = 1;", context);

            Assert.That(atBreak, Is.GreaterThanOrEqualTo(TimeSpan.FromMilliseconds(100)));
            Assert.That(afterWatch, Is.GreaterThanOrEqualTo(atBreak));
        }

        [Test]
        public void NoTimeLimitByDefault()
        {
            var context = CreateContext();

            Assert.That(context.ExecutionLimits.MaxExecutionTime, Is.Null);
            Assert.DoesNotThrow(() => Run("sleep(200); x = 1;", context));
        }

        // ---------------------------------------------------------------------
        // Call depth
        // ---------------------------------------------------------------------

        [Test]
        public void EndlessRecursionThrowsInsteadOfOverflowingTheStack()
        {
            var context = new ParserContext { AssignmentPermissions = AssignmentPermissions.All };

            Assert.Throws<ScriptStackOverflowException>(() => Run("function f(n) { return f(n + 1); } return f(0);", context));
        }

        [Test]
        public void RecursionWithinCallDepthLimitSucceeds()
        {
            var context = new ParserContext { AssignmentPermissions = AssignmentPermissions.All };
            context.ExecutionLimits = new ExecutionLimits { MaxCallDepth = 20 };

            Assert.That(Run<int>("function fac(n) { if (n <= 1) return 1; return n * fac(n - 1); } return fac(10);", context), Is.EqualTo(3628800));
        }

        [Test]
        public void CallDepthLimitIsReported()
        {
            var context = new ParserContext { AssignmentPermissions = AssignmentPermissions.All };
            context.ExecutionLimits = new ExecutionLimits { MaxCallDepth = 10 };

            var ex = Assert.Throws<ScriptStackOverflowException>(() => Run("function f(n) { return f(n + 1); } return f(0);", context))!;

            Assert.That(ex.MaxCallDepth, Is.EqualTo(10));
        }

        [Test]
        public void CallDepthLimitAllowsExactlyItsManyNestedCalls()
        {
            // fac(n) nests n calls, so a limit of 5 is the boundary between these two.
            Assert.That(Run<int>("function fac(n) { if (n <= 1) return 1; return n * fac(n - 1); } return fac(5);", CreateContext(maxCallDepth: 5)), Is.EqualTo(120));
            Assert.Throws<ScriptStackOverflowException>(() => Run("function fac(n) { if (n <= 1) return 1; return n * fac(n - 1); } return fac(6);", CreateContext(maxCallDepth: 5)));
        }

        [Test]
        public void CallDepthRecoversAfterARunIsAborted()
        {
            var context = CreateContext(maxCallDepth: 10);

            Assert.Throws<ScriptStackOverflowException>(() => Run("function f(n) { return f(n + 1); } return f(0);", context));

            // The aborted run unwound through ten nested calls. If any of that depth were
            // left behind, the next run would start part-way down the stack.
            Assert.That(context.ExecutionMonitor!.CallDepth, Is.Zero);
            Assert.That(Run<int>("function fac(n) { if (n <= 1) return 1; return n * fac(n - 1); } return fac(9);", context), Is.EqualTo(362880));
        }
        [Test]
        public void CallDepthIsCountedPerEvaluation()
        {
            var context = new ParserContext { AssignmentPermissions = AssignmentPermissions.All };
            context.ExecutionLimits = new ExecutionLimits { MaxCallDepth = 20 };

            string script = "function fac(n) { if (n <= 1) return 1; return n * fac(n - 1); } return fac(10);";

            for (int i = 0; i < 3; i++)
                Assert.That(Run<int>(script, context), Is.EqualTo(3628800));
        }

        [Test]
        public void CallDepthLimitIgnoresCallsThatHaveReturned()
        {
            var context = new ParserContext { AssignmentPermissions = AssignmentPermissions.All };
            context.ExecutionLimits = new ExecutionLimits { MaxCallDepth = 5 };

            // 100 sequential calls, never more than one deep.
            Assert.That(Run<int>("function inc(n) { return n + 1; } x = 0; foreach (i in [1...100]) x = inc(x); return x;", context), Is.EqualTo(100));
        }

        // ---------------------------------------------------------------------
        // Configuration
        // ---------------------------------------------------------------------

        [Test]
        public void DefaultLimitsCapCallDepthOnly()
        {
            var context = new ParserContext { AssignmentPermissions = AssignmentPermissions.All };

            Assert.That(context.ExecutionLimits.MaxCallDepth, Is.EqualTo(ExecutionLimits.DefaultMaxCallDepth));
            Assert.That(context.ExecutionLimits.MaxExecutionTime, Is.Null);
            Assert.That(context.ExecutionMonitor, Is.Not.Null);
        }

        [Test]
        public void LimitsCanBeDisabledEntirely()
        {
            var context = new ParserContext { AssignmentPermissions = AssignmentPermissions.All };
            context.ExecutionLimits = ExecutionLimits.None;

            Assert.That(context.ExecutionMonitor, Is.Null);
            Assert.That(Run<int>("x = 0; while (x < 100) { x = x + 1; } return x;", context), Is.EqualTo(100));
        }

        [Test]
        public void LocalScopesShareTheOuterMonitor()
        {
            var context = new ParserContext { AssignmentPermissions = AssignmentPermissions.All };
            var local = (ParserContext) context.CreateLocal();

            Assert.That(local.ExecutionMonitor, Is.SameAs(context.ExecutionMonitor));
            Assert.That(local.ExecutionLimits, Is.SameAs(context.ExecutionLimits));
        }

        [Test]
        public void AssigningLimitsReplacesTheMonitor()
        {
            var context = new ParserContext { AssignmentPermissions = AssignmentPermissions.All };
            var original = context.ExecutionMonitor;

            var limits = new ExecutionLimits { MaxExecutionTime = TimeSpan.FromSeconds(1) };

            context.ExecutionLimits = limits;

            Assert.That(context.ExecutionMonitor, Is.Not.SameAs(original));
            Assert.That(context.ExecutionMonitor!.Limits, Is.SameAs(limits));
        }

        [Test]
        public void MonitorIsIdleOutsideAnEvaluation()
        {
            var context = CreateContext();
            context.ExecutionLimits = new ExecutionLimits { MaxExecutionTime = TimeSpan.FromSeconds(10), MaxCallDepth = 20 };

            Assert.That(context.ExecutionMonitor!.Elapsed, Is.EqualTo(TimeSpan.Zero));

            Run("function inc(n) { return n + 1; } sleep(20); x = inc(1);", context);

            // The clock only runs between the start and the end of an evaluation, so a
            // context that is sitting idle reports nothing.
            Assert.That(context.ExecutionMonitor.Elapsed, Is.EqualTo(TimeSpan.Zero));
            Assert.That(context.ExecutionMonitor.CallDepth, Is.Zero);
        }

        [Test]
        public void BothLimitsShareACatchableBaseException()
        {
            var timeLimited = CreateContext();
            timeLimited.ExecutionLimits = new ExecutionLimits { MaxExecutionTime = TimeSpan.FromMilliseconds(20) };

            var depthLimited = CreateContext(maxCallDepth: 5);

            // A host that just wants to abort runaway scripts can catch the one base type.
            Assert.Catch<ScriptExecutionLimitException>(() => Run("sleep(150); x = 1;", timeLimited));
            Assert.Catch<ScriptExecutionLimitException>(() => Run("function f(n) { return f(n + 1); } return f(0);", depthLimited));
        }

        [Test]
        public void LimitsDoNotDisturbNormalExpressionEvaluation()
        {
            Assert.That(new CSharpParser().Evaluate<int>("2 + 3 * 4"), Is.EqualTo(14));
        }
    }
}
