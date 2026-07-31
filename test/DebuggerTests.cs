//=============================================================================
// Iridium Script - Portable .NET Productivity Library 
//
// Tests for the script debugging hooks (breakpoints, variable evaluation,
// stepping and the call stack).
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
    public class DebuggerTests
    {
        private class Debuggee
        {
            public CScriptParser Parser;
            public ParserContext Context;
            public ScriptDebugger Debugger;
            public StringBuilder Output;

            public IValueWithType Run(string script) => Parser.Evaluate(script);
        }

        private static Debuggee CreateDebuggee()
        {
            var output = new StringBuilder();

            var context = new ParserContext { AssignmentPermissions = AssignmentPermissions.All };

            context.Set("print", new Action<object>(o => output.Append(o)));

            var debugger = new ScriptDebugger();

            context.Debugger = debugger;

            var parser = new CScriptParser { DefaultContext = context };

            return new Debuggee { Parser = parser, Context = context, Debugger = debugger, Output = output };
        }

        // ---------------------------------------------------------------------
        // Breakpoints
        // ---------------------------------------------------------------------

        [Test]
        public void BreakpointStopsAtItsLine()
        {
            var d = CreateDebuggee();
            var script =
                "a = 1;\n" + // line 1
                "b = 2;\n" + // line 2
                "c = 3;";    // line 3

            d.Debugger.Breakpoints.Add(2);

            SourceSpan? stoppedAt = null;
            ScriptBreakReason reason = ScriptBreakReason.Step;

            d.Debugger.Break += (s, e) =>
            {
                stoppedAt = e.Location;
                reason = e.Reason;
                e.Continue();
            };

            d.Run(script);

            Assert.IsTrue(stoppedAt.HasValue);
            Assert.AreEqual(2, stoppedAt.Value.Start.Line);
            Assert.AreEqual(ScriptBreakReason.Breakpoint, reason);
        }

        [Test]
        public void NoBreakpointsRunsToCompletionWithoutBreaking()
        {
            var d = CreateDebuggee();

            bool broke = false;
            d.Debugger.Break += (s, e) => { broke = true; e.Continue(); };

            d.Run("a = 1;\nb = 2;");

            Assert.IsFalse(broke);
        }

        [Test]
        public void DisabledBreakpointDoesNotStop()
        {
            var d = CreateDebuggee();

            var bp = d.Debugger.Breakpoints.Add(2);
            bp.Enabled = false;

            bool broke = false;
            d.Debugger.Break += (s, e) => { broke = true; e.Continue(); };

            d.Run("a = 1;\nb = 2;\nc = 3;");

            Assert.IsFalse(broke);
        }

        [Test]
        public void MultipleBreakpointsAreHitInOrderOnContinue()
        {
            var d = CreateDebuggee();

            d.Debugger.Breakpoints.Add(1);
            d.Debugger.Breakpoints.Add(3);

            var hitLines = new List<int>();
            d.Debugger.Break += (s, e) => { hitLines.Add(e.Location.Start.Line); e.Continue(); };

            d.Run("a = 1;\nb = 2;\nc = 3;");

            CollectionAssert.AreEqual(new[] { 1, 3 }, hitLines);
        }

        [Test]
        public void BreakpointHitCountIncrementsPerIteration()
        {
            var d = CreateDebuggee();

            var script =
                "i = 0;\n" +          // line 1
                "while (i < 3) {\n" + // line 2
                "  i = i + 1;\n" +    // line 3
                "}";                  // line 4

            var bp = d.Debugger.Breakpoints.Add(3);

            d.Debugger.Break += (s, e) => e.Continue();

            d.Run(script);

            Assert.AreEqual(3, bp.HitCount);
        }

        [Test]
        public void ConditionalBreakpointStopsOnlyWhenConditionIsTrue()
        {
            var d = CreateDebuggee();

            var script =
                "i = 0;\n" +           // line 1
                "while (i < 10) {\n" + // line 2
                "  i = i + 1;\n" +     // line 3
                "}";                   // line 4

            d.Debugger.Breakpoints.Add(3, "i == 5");

            var stops = new List<object>();
            d.Debugger.Break += (s, e) => { stops.Add(e.Evaluate("i")); e.Continue(); };

            d.Run(script);

            // Should have paused exactly once, when i was 5 (before the increment).
            Assert.AreEqual(1, stops.Count);
            Assert.AreEqual(5, stops[0]);
        }

        // ---------------------------------------------------------------------
        // Variable / expression evaluation while stopped
        // ---------------------------------------------------------------------

        [Test]
        public void CanEvaluateVariablesAtBreakpoint()
        {
            var d = CreateDebuggee();

            var script =
                "x = 41;\n" +    // line 1
                "y = x + 1;\n" + // line 2
                "z = 0;";        // line 3

            d.Debugger.Breakpoints.Add(3);

            object x = null, y = null;
            d.Debugger.Break += (s, e) =>
            {
                x = e.Evaluate("x");
                y = e.Evaluate("y");
                e.Continue();
            };

            d.Run(script);

            Assert.AreEqual(41, x);
            Assert.AreEqual(42, y);
        }

        [Test]
        public void CanEvaluateWatchExpressionAtBreakpoint()
        {
            var d = CreateDebuggee();

            d.Debugger.Breakpoints.Add(3);

            object sum = null;
            d.Debugger.Break += (s, e) => { sum = e.Evaluate("x + y"); e.Continue(); };

            d.Run("x = 10;\ny = 20;\nz = 0;");

            Assert.AreEqual(30, sum);
        }

        [Test]
        public void TryEvaluateReturnsFalseOnError()
        {
            var d = CreateDebuggee();

            d.Debugger.Breakpoints.Add(2);

            bool ok = true;
            object value = "unset";
            d.Debugger.Break += (s, e) => { ok = e.TryEvaluate("1 * *", out value); e.Continue(); };

            d.Run("a = 1;\nb = 2;");

            Assert.IsFalse(ok);
            Assert.IsNull(value);
        }

        [Test]
        public void GetVariablesInScopeReturnsAssignedLocals()
        {
            var d = CreateDebuggee();

            var script =
                "a = 1;\n" + // line 1
                "b = 2;\n" + // line 2
                "c = 3;";    // line 3

            d.Debugger.Breakpoints.Add(3);

            Dictionary<string, object> vars = null;
            d.Debugger.Break += (s, e) =>
            {
                vars = e.GetVariablesInScope().ToDictionary(v => v.Name, v => v.Value);
                e.Continue();
            };

            d.Run(script);

            Assert.AreEqual(1, vars["a"]);
            Assert.AreEqual(2, vars["b"]);
            Assert.IsFalse(vars.ContainsKey("c")); // not assigned yet when stopped on line 3
        }

        [Test]
        public void WatchEvaluationDoesNotTriggerNestedBreakpoints()
        {
            var d = CreateDebuggee();

            d.Debugger.Breakpoints.Add(2);

            int breakCount = 0;
            d.Debugger.Break += (s, e) =>
            {
                breakCount++;
                // Evaluating an expression that spans "line 2" must not re-enter the debugger.
                e.Evaluate("a + 1");
                e.Continue();
            };

            d.Run("a = 1;\nb = 2;\nc = 3;");

            Assert.AreEqual(1, breakCount);
        }

        // ---------------------------------------------------------------------
        // Stepping
        // ---------------------------------------------------------------------

        [Test]
        public void StepIntoVisitsEachStatementInOrder()
        {
            var d = CreateDebuggee();

            var script =
                "a = 1;\n" + // line 1
                "b = 2;\n" + // line 2
                "c = 3;";    // line 3

            d.Debugger.Breakpoints.Add(1);

            var lines = new List<int>();
            d.Debugger.Break += (s, e) => { lines.Add(e.Location.Start.Line); e.StepInto(); };

            d.Run(script);

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, lines);
        }

        [Test]
        public void StepOverDoesNotDescendIntoNestedStatements()
        {
            var d = CreateDebuggee();

            var script =
                "a = 1;\n" +        // line 1
                "if (a == 1) {\n" + // line 2
                "  a = 2;\n" +      // line 3 (nested)
                "}\n" +
                "b = 3;";           // line 5

            d.Debugger.Breakpoints.Add(2);

            var lines = new List<int>();
            d.Debugger.Break += (s, e) => { lines.Add(e.Location.Start.Line); e.StepOver(); };

            d.Run(script);

            // Stepping over the 'if' should land on the next sibling statement (line 5),
            // not the nested body (line 3).
            CollectionAssert.AreEqual(new[] { 2, 5 }, lines);
        }

        // ---------------------------------------------------------------------
        // Call stack
        // ---------------------------------------------------------------------

        [Test]
        public void CallStackReflectsFunctionNesting()
        {
            var d = CreateDebuggee();

            var script =
                "function f() {\n" + // line 1
                "  x = 1;\n" +       // line 2 (function body)
                "}\n" +
                "f();";              // line 4

            d.Debugger.Breakpoints.Add(2);

            int depth = 0;
            SourceSpan innerFrame = default;
            SourceSpan outerFrame = default;
            d.Debugger.Break += (s, e) =>
            {
                depth = e.CallStack.Count;
                innerFrame = e.CallStack[0].Location;
                outerFrame = e.CallStack[e.CallStack.Count - 1].Location;
                e.Continue();
            };

            d.Run(script);

            Assert.GreaterOrEqual(depth, 2);
            Assert.AreEqual(2, innerFrame.Start.Line); // innermost frame is the function body
            Assert.AreEqual(4, outerFrame.Start.Line); // outermost frame is the call site
        }

        // ---------------------------------------------------------------------
        // Pause / Stop
        // ---------------------------------------------------------------------

        [Test]
        public void PauseBreaksBeforeNextStatement()
        {
            var d = CreateDebuggee();

            d.Debugger.Pause();

            int? firstLine = null;
            ScriptBreakReason reason = ScriptBreakReason.Breakpoint;
            d.Debugger.Break += (s, e) =>
            {
                if (firstLine == null)
                {
                    firstLine = e.Location.Start.Line;
                    reason = e.Reason;
                }
                e.Continue();
            };

            d.Run("a = 1;\nb = 2;");

            Assert.AreEqual(1, firstLine);
            Assert.AreEqual(ScriptBreakReason.Pause, reason);
        }

        [Test]
        public void StopTerminatesScriptExecution()
        {
            var d = CreateDebuggee();

            var script =
                "a = 1;\n" + // line 1 (runs)
                "b = 2;\n" + // line 2 (breakpoint, before execution)
                "c = 3;";    // line 3 (never runs)

            d.Debugger.Breakpoints.Add(2);
            d.Debugger.Break += (s, e) => e.Stop();

            Assert.Throws<ScriptTerminatedException>(() => d.Run(script));

            Assert.IsTrue(d.Context.Exists("a"));
            Assert.IsFalse(d.Context.Exists("c"));
        }

        // ---------------------------------------------------------------------
        // Non-intrusiveness
        // ---------------------------------------------------------------------

        [Test]
        public void ScriptWithoutDebuggerBehavesNormally()
        {
            var output = new StringBuilder();
            var context = new ParserContext { AssignmentPermissions = AssignmentPermissions.All };
            context.Set("print", new Action<object>(o => output.Append(o)));

            var parser = new CScriptParser { DefaultContext = context };

            var result = parser.Evaluate<int>("x = 6;\ny = 7;\nreturn x * y;");

            Assert.AreEqual(42, result);
        }
    }
}
