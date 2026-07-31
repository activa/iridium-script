# Debugging Iridium scripts

Iridium.Script can hand control to a **debugger** while a script runs, so a host
application — typically an IDE or a rule editor — can offer breakpoints, stepping,
a call stack, and live variable/expression evaluation while execution is paused.

The library provides the **hooks**; it deliberately does *not* provide any UI.
Everything here is designed so you can build a debugging front-end (WinForms, WPF,
Avalonia, a web editor, a REPL, …) on top of it.

> Debugging is entirely opt-in. If you never attach a debugger, scripts run exactly
> as before with no measurable overhead and no behavioral change.

---

## Table of contents

- [How it works](#how-it-works)
- [Quick start](#quick-start)
- [Setting breakpoints](#setting-breakpoints)
- [Handling a break](#handling-a-break)
- [Evaluating variables and watch expressions](#evaluating-variables-and-watch-expressions)
- [Inspecting locals and the call stack](#inspecting-locals-and-the-call-stack)
- [Stepping](#stepping)
- [Pausing and stopping](#pausing-and-stopping)
- [Source locations](#source-locations)
- [Building a real UI: the threading model](#building-a-real-ui-the-threading-model)
- [API reference](#api-reference)
- [Limitations & notes](#limitations--notes)

---

## How it works

Every node in a parsed script carries a [`SourceSpan`](#source-locations) that maps it
back to the exact region of source it came from. When a `ScriptDebugger` is attached
to the execution context, the engine gives it a chance to pause **before each
statement executes**, passing along the statement's location and the current scope.

The unit of breakpointing and stepping is the **statement** — an assignment, an
expression statement, a `return`/`break`, an `if`/`while`/`foreach` header, or a
function-call statement. Container nodes (statement blocks) are transparent to the
debugger, so only real statements trigger breaks.

Three pieces cooperate:

| Type | Role |
| --- | --- |
| `ScriptDebugger` | Holds breakpoints, tracks the call stack, decides when to pause, raises the `Break` event. |
| `ParserContext.Debugger` | Where you attach the debugger. It propagates automatically to nested scopes (loops, function calls). |
| `ScriptDebugBreakEventArgs` | Handed to you while paused: location, reason, call stack, plus methods to evaluate variables and choose how to resume. |

---

## Quick start

Attach a `ScriptDebugger` to your context, set a breakpoint, and handle the `Break`
event. This minimal (synchronous) example just prints state and continues:

```csharp
using Iridium.Script;
using Iridium.Script.CSharp;

var context = new ParserContext { AssignmentPermissions = AssignmentPermissions.All };

var debugger = new ScriptDebugger();
context.Debugger = debugger;                 // <-- enables debugging for this execution

debugger.Breakpoints.Add(3);                 // break on line 3

debugger.Break += (sender, e) =>
{
    Console.WriteLine($"Paused at {e.Location} because of {e.Reason}");
    Console.WriteLine($"  total = {e.Evaluate("total")}");   // evaluate a variable in scope
    e.Continue();                            // resume execution
};

var parser = new CScriptParser { DefaultContext = context };

parser.Evaluate(@"
price = 20.0m;
qty   = 3;
total = price * qty;      // line 4 in this string (line 1 is blank)
");
```

The `Break` event fires **synchronously on the thread running the script**;
execution stays suspended until your handler returns. For a real UI you will block
that thread until the user acts — see
[Building a real UI](#building-a-real-ui-the-threading-model).

---

## Setting breakpoints

Breakpoints live in `debugger.Breakpoints` (a `BreakpointCollection`) and are keyed
by **one-based source line**. At most one breakpoint exists per line.

```csharp
debugger.Breakpoints.Add(12);                    // break when a statement on line 12 runs
debugger.Breakpoints.Add(20, "count > 100");     // conditional breakpoint
debugger.Breakpoints.Toggle(12);                 // add if absent, remove if present (great for gutter clicks)
debugger.Breakpoints.Remove(20);
debugger.Breakpoints.Clear();

bool isSet = debugger.Breakpoints.Contains(12);
Breakpoint bp = debugger.Breakpoints[12];        // null if none
```

Each `Breakpoint` exposes:

| Member | Meaning |
| --- | --- |
| `Line` | One-based line the breakpoint is on. |
| `Enabled` | Toggle without removing it. |
| `Condition` | Optional boolean expression; execution only pauses when it is truthy in the current scope. |
| `HitCount` | How many times execution has paused on it. |

```csharp
var bp = debugger.Breakpoints.Add(30);
bp.Condition = "order.Total > 1000";   // only stop for large orders
bp.Enabled   = false;                  // temporarily disable
```

A conditional breakpoint is evaluated with the same engine that runs the script, in
the paused scope, and debugging is suppressed during that evaluation (it can't
recurse). If a condition throws, the debugger errs on the side of stopping so the
mistake is noticed.

---

## Handling a break

Subscribe to `ScriptDebugger.Break`. The `ScriptDebugBreakEventArgs` describes the
pause and lets you decide how to resume:

```csharp
debugger.Break += (sender, e) =>
{
    SourceSpan       where  = e.Location;      // where execution is about to run
    ScriptBreakReason why    = e.Reason;       // Breakpoint / Step / Pause
    Breakpoint       hit    = e.Breakpoint;    // the breakpoint (null unless Reason == Breakpoint)
    IParserContext   scope  = e.Context;       // the current scope

    // ...inspect state, then choose exactly one resume action:
    e.Continue();     // run to the next breakpoint
    // e.StepInto();  // step into the next statement / call
    // e.StepOver();  // step over nested blocks and calls
    // e.StepOut();   // run until the current block/function returns
    // e.Stop();      // abort the script (throws ScriptTerminatedException)
};
```

If there is no `Break` subscriber, execution simply continues (the default resume
action is `Continue`). Setting the action explicitly through `e.ResumeAction` is
equivalent to calling the helper methods.

---

## Evaluating variables and watch expressions

While paused you can evaluate any expression — a bare variable name or an arbitrary
expression — in the current scope. This is the "watch"/"immediate window" capability.

```csharp
debugger.Break += (sender, e) =>
{
    object total   = e.Evaluate("total");            // a variable
    decimal withVat = e.Evaluate<decimal>("total * 1.21m"); // typed watch expression
    bool   big     = e.Evaluate<bool>("total > 1000");

    // Watch panels want to survive bad input (unknown names, syntax errors):
    if (e.TryEvaluate("customer.Name", out var name))
        Console.WriteLine($"customer = {name}");

    e.Continue();
};
```

`Evaluate` runs against `e.Context`, so it sees exactly the variables the script
sees at that point (including outer scopes). Debugging is suppressed during watch
evaluation, so watches never trigger nested breakpoints.

---

## Inspecting locals and the call stack

For a "Locals" panel, enumerate everything in scope. Inner scopes shadow outer ones:

```csharp
debugger.Break += (sender, e) =>
{
    foreach (DebugVariable v in e.GetVariablesInScope())
        Console.WriteLine($"{v.Name} : {v.Type?.Name} = {v.Value}");

    e.Continue();
};
```

For a "Call stack" panel, read `e.CallStack`. Frames are ordered innermost first —
the currently executing statement is `CallStack[0]` and the outermost frame is last,
which is the order a debugger UI lists them in. Each `DebugStackFrame` has a
`Location` and the `Context` (scope) it runs in — so you can evaluate variables
*per frame* if you want:

```csharp
debugger.Break += (sender, e) =>
{
    for (int i = 0; i < e.CallStack.Count; i++)
        Console.WriteLine($"#{i}  {e.CallStack[i].Location}");

    e.Continue();
};
```

Calling a user-defined `function` pushes a frame, so stepping into a function and
inspecting its stack works as you'd expect.

---

## Stepping

Choose a stepping action while paused; the debugger stops again at the appropriate
next statement:

| Action | Behavior |
| --- | --- |
| `StepInto` | Stop at the very next statement, descending into nested blocks and function calls. |
| `StepOver` | Stop at the next statement at the same nesting level, skipping nested blocks/calls. |
| `StepOut` | Run until execution returns to a shallower level, then stop. |
| `Continue` | Run until the next breakpoint (or the script ends). |

A simple line-tracer that single-steps the whole script:

```csharp
var visited = new List<int>();

debugger.Breakpoints.Add(1);                 // stop on the first statement
debugger.Break += (sender, e) =>
{
    visited.Add(e.Location.Start.Line);
    e.StepInto();                            // then walk statement by statement
};
```

---

## Pausing and stopping

**Pause** (a "break all" button): request a pause and execution stops before the
next statement runs, with `Reason == ScriptBreakReason.Pause`.

```csharp
debugger.Pause();   // typically called from your UI thread while a script is running
```

**Stop** (an "abort" button): from within a break handler, call `e.Stop()`. This
throws `ScriptTerminatedException` to unwind the running script. Catch it where you
invoked evaluation:

```csharp
debugger.Break += (sender, e) => e.Stop();

try
{
    parser.Evaluate(script);
}
catch (ScriptTerminatedException)
{
    // user aborted — expected
}
```

---

## Source locations

Locations use two small value types (see also the error-reporting feature, which
uses the same types to report the line/column of parse errors):

```csharp
public readonly struct SourcePosition   // Index (0-based), Line (1-based), Column (1-based), IsKnown
public readonly struct SourceSpan       // Start, End (SourcePosition), IsKnown, Line
```

Every `Expression` exposes a `SourceSpan` (`SourceSpan.Unknown` when not applicable).
The parser fills it in for statements and control-flow constructs, which is what the
debugger uses to place breakpoints and highlight the current line:

```csharp
var location = e.Location;                 // SourceSpan of the statement about to run
int line   = location.Start.Line;          // for the editor gutter/highlight
int col    = location.Start.Column;
```

---

## Building a real UI: the threading model

The `Break` event is raised **synchronously on the thread that is executing the
script**, and execution remains paused for as long as your handler runs. A UI
therefore typically:

1. Runs the script on a **background thread** (so the UI stays responsive).
2. Inside the `Break` handler, **marshals the paused state to the UI thread** and
   then **blocks the script thread** until the user chooses how to resume.
3. Services variable/watch requests **on the script thread** (the context is not
   thread-safe, so do not touch `e.Context` from the UI thread directly).

The small adapter below turns the synchronous hook into an `async` API that a UI can
drive comfortably. It exposes a `Task` that completes when the script pauses, runs
watch/step commands on the script thread, and resumes on demand.

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Iridium.Script;
using Iridium.Script.CSharp;

/// <summary>
/// Bridges the synchronous ScriptDebugger.Break hook to an async, UI-friendly API.
/// Run the script with RunAsync(); await Paused to learn when it stops; call
/// Evaluate(...) while paused; call Resume(...) to continue.
/// </summary>
public sealed class InteractiveDebugSession
{
    private readonly ScriptDebugger _debugger;
    private readonly SemaphoreSlim _resumeSignal = new(0, 1);
    private readonly object _gate = new();

    private ScriptDebugBreakEventArgs _current;             // valid only while paused
    private TaskCompletionSource<ScriptDebugBreakEventArgs> _paused;

    public InteractiveDebugSession(ScriptDebugger debugger)
    {
        _debugger = debugger;
        _debugger.Break += OnBreak;
    }

    public BreakpointCollection Breakpoints => _debugger.Breakpoints;
    public bool IsPaused { get; private set; }

    /// <summary>Completes each time the script pauses. Re-read after every Resume().</summary>
    public Task<ScriptDebugBreakEventArgs> Paused
    {
        get
        {
            lock (_gate)
            {
                _paused ??= new TaskCompletionSource<ScriptDebugBreakEventArgs>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                return _paused.Task;
            }
        }
    }

    /// <summary>Runs the script on a background thread. The returned task completes when it finishes.</summary>
    public Task RunAsync(Func<object> run) => Task.Run(run);

    // --- called on the SCRIPT thread while paused ---

    private void OnBreak(object sender, ScriptDebugBreakEventArgs e)
    {
        lock (_gate)
        {
            _current = e;
            IsPaused = true;
            var tcs = _paused ?? new TaskCompletionSource<ScriptDebugBreakEventArgs>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            _paused = null;
            tcs.TrySetResult(e);      // notify the UI that we stopped
        }

        _resumeSignal.Wait();         // block the script thread until Resume() is called

        lock (_gate) { IsPaused = false; _current = null; }
    }

    // --- called from the UI while paused; executed on the script thread ---

    /// <summary>
    /// Evaluate a watch expression. Must be marshalled so it runs where the script
    /// thread is blocked. Here we simply call e.Evaluate directly, which is safe
    /// because the script thread is parked inside OnBreak and not mutating state.
    /// </summary>
    public bool TryEvaluate(string expression, out object value)
    {
        lock (_gate)
        {
            if (_current == null) { value = null; return false; }
            return _current.TryEvaluate(expression, out value);
        }
    }

    public void Resume(DebugResumeAction action)
    {
        lock (_gate)
        {
            if (_current == null) return;
            _current.ResumeAction = action;
        }

        _resumeSignal.Release();
    }
}
```

Usage from a UI (pseudo-code):

```csharp
var context  = new ParserContext { AssignmentPermissions = AssignmentPermissions.All };
var debugger = new ScriptDebugger();
context.Debugger = debugger;

var session = new InteractiveDebugSession(debugger);
session.Breakpoints.Add(10);

var parser = new CScriptParser { DefaultContext = context };
var runTask = session.RunAsync(() => parser.Evaluate(script));

// UI loop:
while (!runTask.IsCompleted)
{
    var pause = await Task.WhenAny(session.Paused, runTask) == runTask ? null : await session.Paused;
    if (pause == null) break;

    HighlightLine(pause.Location.Start.Line);        // update the editor on the UI thread
    RefreshLocals(pause.GetVariablesInScope());
    if (session.TryEvaluate(userWatchText, out var v))
        ShowWatch(v);

    // when the user clicks a toolbar button:
    session.Resume(DebugResumeAction.StepOver);
}

try { await runTask; }
catch (ScriptTerminatedException) { /* user hit Stop */ }
```

> The adapter above is intentionally minimal and illustrative. If your UI needs to
> evaluate watches *while the user is looking at the paused state* (rather than at
> the moment of pausing), route those requests to the script thread via a small
> command queue that the `OnBreak` method pumps while it waits on `_resumeSignal`.
> The key rule is: **only touch `e.Context` / `e.Evaluate` from the script thread.**

---

## API reference

**Attach a debugger**

```csharp
var debugger = new ScriptDebugger();                 // uses CSharpParser.Default for conditions/watches
var debugger2 = new ScriptDebugger(customParser);    // use a specific parser for conditions/watches
context.Debugger = debugger;                          // ParserContext.Debugger (propagates to child scopes)
```

**`ScriptDebugger`**

```csharp
BreakpointCollection Breakpoints { get; }
bool                 IsEnabled  { get; set; }         // false => debugger stays fully out of the way
event EventHandler<ScriptDebugBreakEventArgs> Break;  // raised while paused
IReadOnlyList<DebugStackFrame> GetCallStack();        // innermost first; copies the stack, paused-only
SourceSpan           CurrentLocation { get; }
void                 Pause();                          // break before the next statement
protected virtual void OnBreak(ScriptDebugBreakEventArgs e);  // override instead of subscribing, if preferred
```

**`BreakpointCollection`**

```csharp
Breakpoint Add(int line);
Breakpoint Add(int line, string condition);
void       Add(Breakpoint breakpoint);
bool       Remove(int line);
Breakpoint Toggle(int line);        // returns the added breakpoint, or null if removed
bool       Contains(int line);
bool       TryGet(int line, out Breakpoint breakpoint);
Breakpoint this[int line] { get; }  // null if none
void       Clear();
int        Count { get; }
// IEnumerable<Breakpoint>
```

**`Breakpoint`**

```csharp
int    Line     { get; }
bool   Enabled  { get; set; }
string Condition{ get; set; }
int    HitCount { get; }
```

**`ScriptDebugBreakEventArgs`**

```csharp
SourceSpan                     Location   { get; }
ScriptBreakReason              Reason     { get; }   // Breakpoint | Step | Pause
Breakpoint                     Breakpoint { get; }   // null unless Reason == Breakpoint
IParserContext                 Context    { get; }
IReadOnlyList<DebugStackFrame> CallStack  { get; }   // innermost frame first
DebugResumeAction              ResumeAction { get; set; }

void Continue();  void StepInto();  void StepOver();  void StepOut();  void Stop();

object                     Evaluate(string expression);
T                          Evaluate<T>(string expression);
bool                       TryEvaluate(string expression, out object value);
IEnumerable<DebugVariable> GetVariablesInScope();
```

**Supporting types**

```csharp
enum ScriptBreakReason { Breakpoint, Step, Pause }
enum DebugResumeAction { Continue, StepInto, StepOver, StepOut, Stop }
class DebugStackFrame { SourceSpan Location; IParserContext Context; }
class DebugVariable   { string Name; object Value; Type Type; }
class ScriptTerminatedException : Exception   // thrown by Stop()
interface IScriptDebugger    // the runtime hook; implemented by ScriptDebugger
interface IDebuggableContext // implemented by ParserContext; carries the debugger
```

---

## Limitations & notes

- **Statement granularity.** Breakpoints and stepping operate on statements. A
  breakpoint on a blank line, a comment, or the closing `}` of a block will not fire;
  put it on a line that contains an executable statement.
- **Loop headers pause once.** A breakpoint on a `while`/`foreach` header stops when
  the loop is first reached; iterations then break on the statements inside the body.
- **Stepping is nesting-based.** `StepOver` skips statements nested more deeply than
  the current one (including function-call bodies). For most scripts this matches
  intuition; if you need a different policy, you can implement it in your host using
  `e.CallStack` depth plus `Continue`/breakpoints.
- **Threading.** The `Break` event is synchronous on the evaluation thread, and the
  execution context is not thread-safe. Evaluate watches and read `Context` only on
  that thread (see the adapter above).
- **Not a sandbox.** The debugger controls *when* code runs, not *what* it may do.
  Use `AssignmentPermissions` and the context you expose to constrain scripts.
- **Zero-cost when unused.** With no `Debugger` attached (or `IsEnabled = false`),
  statement execution is a direct passthrough — there is no behavioral or measurable
  performance difference from non-debugged evaluation.
```
