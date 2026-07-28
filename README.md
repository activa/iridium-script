# Iridium.Script

**A portable, lightweight C#-style expression evaluator, mini scripting engine, and template renderer for .NET.**

Iridium.Script lets you take a *string* like `"order.Total > 100 ? order.Total * 0.9m : order.Total"` and turn it into a real, evaluated value at runtime — using your own .NET objects, methods, and types as first-class citizens. It does this **without `Reflection.Emit` or runtime code generation**, so it runs anywhere .NET runs, including AOT-compiled and mobile targets (Xamarin / MAUI / iOS) where dynamic IL is not allowed.

It ships three complementary engines in a single small assembly:

| Engine | What it does |
| --- | --- |
| **Expression evaluator** | Parses and evaluates a single C#-like expression against a set of variables/objects. |
| **Script runner** | Runs lightweight, C#-flavored scripts with `if`/`while`/`foreach`, blocks, `return`, and user-defined `function`s. |
| **Template renderer** | Renders text/HTML/XML templates with embedded expressions, loops, conditionals, and macros in several syntaxes (Velocity, `{{ }}`, HTML-comment, XML). |

---

## A taste of what it can do

The examples throughout this document use a small e-commerce domain model. Imagine these are your ordinary application classes:

```csharp
class Customer
{
    public string Name;
    public string Country;      // ISO code, e.g. "BE"
    public bool   IsPremium;
    public bool   IsBlocked;
    public IList<Order> Orders;
}

class Order
{
    public int        Id;
    public string     Status;        // "pending", "paid", "shipped", ...
    public decimal    Total;
    public DateTime?  ShippedDate;    // null until shipped
    public Customer   Customer;
    public OrderLine[] Lines;
}

class OrderLine
{
    public string  Product;
    public int     Quantity;
    public decimal UnitPrice;
    public decimal LineTotal => Quantity * UnitPrice;
}
```

The power of the library is that expressions, scripts and templates operate directly on instances of *these* objects — no wrappers, no code generation.

### 1. Evaluate a business rule that lives in your database

Let a non-developer author a discount rule in your back-office and store it as text. Evaluate it at runtime against a real `Order`:

```csharp
using Iridium.Script.CSharp;

// Loaded from config / a rules table — changeable without a redeploy:
string rule = "order.Customer.IsPremium && order.Total >= 100 ? order.Total * 0.9m : order.Total";

var context = new ParserContext();
context.Set("order", order);

decimal amountToCharge = new CSharpParser().Evaluate<decimal>(rule, context);
```

### 2. Decide eligibility with a dynamic filter

Feature flags, promotion targeting, segmentation — all as editable boolean expressions:

```csharp
var parser = new CSharpParser { DefaultContext = context };

bool eligibleForPromo = parser.Evaluate<bool>(
    "customer.Country == \"BE\" && customer.Orders.Count > 3 && !customer.IsBlocked");
```

### 3. Run a small script over an order

Scripts add loops, conditionals and variables — perfect for calculations that are more than a one-liner:

```csharp
using Iridium.Script;
using Iridium.Script.CSharp;

var context = new FlexContext { AssignmentPermissions = AssignmentPermissions.All };
context.Set("order", order);

decimal grandTotal = new CScriptParser().Evaluate<decimal>(@"
    subtotal = 0.0m;

    foreach (line in order.Lines)
        subtotal = subtotal + line.Quantity * line.UnitPrice;

    // Free shipping over 50, otherwise a flat rate:
    shipping = subtotal >= 50 ? 0.0m : 4.95m;

    return subtotal + shipping;
", context);
```

### 4. Define reusable functions (with recursion)

```csharp
double projected = new CScriptParser().Evaluate<double>(@"
    // Compound growth: value after N years at a fixed rate.
    function futureValue(principal, rate, years)
    {
        if (years == 0)
            return principal;

        return futureValue(principal * (1 + rate), rate, years - 1);
    }

    return futureValue(1000.0, 0.05, 3);
");
// => 1157.625
```

### 5. Render an order-confirmation email from a template

```csharp
using Iridium.Script;

var parser = new TemplateParser<DoubleCurly>();

string email = parser.Render(@"
Hi {{order.Customer.Name}},

Thanks for order #{{order.Id}}! You ordered:
{{foreach line in order.Lines}}  - {{line.Quantity}} x {{line.Product}} ({{line.LineTotal ` 0.00}})
{{end}}
Order total: {{order.Total ` 0.00}}
{{if order.Total >= 50}}Good news — shipping is on us!{{end}}
", context);
```

### 6. Run rules straight against an incoming JSON payload

A webhook or message-queue payload arrives as JSON — evaluate against it with no mapping step:

```csharp
using Iridium.Json;
using Iridium.Script.CSharp;

var payload = JsonParser.Parse(webhookBody);   // e.g. {"event":"order.created","data":{"amount":1500}}
var context = new FlexContext(payload);         // the JSON object becomes the root

bool needsReview = new CSharpParser().Evaluate<bool>(
    "event == \"order.created\" && data.amount > 1000", context);
```

If any of that looks useful, read on.

---

## Table of contents

- [Installation & requirements](#installation--requirements)
- [Core concepts](#core-concepts)
- [The expression language](#the-expression-language)
- [The scripting language](#the-scripting-language)
- [Contexts: variables, types, functions & objects](#contexts-variables-types-functions--objects)
- [Template rendering](#template-rendering)
- [Building expression trees by hand](#building-expression-trees-by-hand)
- [Error handling](#error-handling)
- [How it works (architecture)](#how-it-works-architecture)
- [API cheat-sheet](#api-cheat-sheet)

---

## Installation & requirements

Iridium.Script is distributed as the NuGet package **`iridium.script`**.

```
dotnet add package iridium.script
```

- **Target frameworks:** `netstandard2.0`, `netstandard2.1`, `net8.0`, `net9.0`.
- **Dependencies:** `iridium.convert` (for robust type conversion). The JSON examples additionally use `iridium.json`.
- **License:** MIT.
- **AOT/mobile friendly:** all evaluation is done via reflection and a hand-written evaluator — never `Reflection.Emit` — so it works on platforms that forbid runtime IL generation.

The two namespaces you will use most are:

```csharp
using Iridium.Script;         // contexts, templates, expression trees
using Iridium.Script.CSharp;  // CSharpParser / CScriptParser
```

---

## Core concepts

Everything in the library revolves around three building blocks:

1. **A parser** (`CSharpParser` or `CScriptParser`) turns a string into an evaluatable expression tree.
2. **A context** (`ParserContext`, `FlexContext`, or a `DynamicObject`) supplies the variables, .NET types, functions, and root objects that expressions can reference.
3. **An evaluation** produces a value — either strongly typed, as `object`, or as an `IValueWithType` that carries both the value *and* its static type.

```csharp
var parser  = new CSharpParser();
var context = new ParserContext();
context.Set("unitPrice", 9.99);
context.Set("quantity", 3);

double total          = parser.Evaluate<double>("unitPrice * quantity", context);  // 29.97, strongly typed
object boxed          = parser.EvaluateToObject("unitPrice * quantity", context);  // 29.97 as object
IValueWithType typed  = parser.Evaluate("unitPrice * quantity", context);          // .Value == 29.97, .Type == typeof(double)
object val            = parser.Evaluate("unitPrice * quantity", out Type t, context); // value + out Type
```

Each parser also carries a `DefaultContext`, so if you set it once you can omit the context on every call:

```csharp
var parser = new CSharpParser { DefaultContext = context };
parser.Evaluate<double>("unitPrice * quantity");   // uses DefaultContext
```

There is also a ready-made shared instance for one-off, context-light use:

```csharp
CSharpParser.Default.Evaluate("customer.IsPremium", context);
```

### Parse once, evaluate many

`Evaluate(...)` parses every time. For hot paths — e.g. applying the same formula to every row of a report — parse once and re-evaluate against changing contexts:

```csharp
var parser = new CSharpParser();

// Parse the formula to a reusable expression tree:
var formula = parser.Parse("unitPrice * quantity * (1 - discount)");

// Re-evaluate it for each line item, as often as you like:
foreach (var line in lines)
{
    var lineContext = new ParserContext(line);   // line exposes unitPrice/quantity/discount
    double lineTotal = formula.Evaluate<double>(lineContext);
}
```

---

## The expression language

The expression syntax closely mirrors C#. The following are all evaluated exactly as they would be in C#.

### Literals

```csharp
new CSharpParser().Evaluate<string>("\"xyz\"");        // string literals with escapes: \n \t \f \" \x45 ...
new CSharpParser().Evaluate<char>("'\\n'");            // char literals, incl. \xNN hex escapes
new CSharpParser().EvaluateToObject("123");            // int
new CSharpParser().EvaluateToObject("123L");           // long   (suffixes L/l/U/u and combinations)
new CSharpParser().EvaluateToObject("123LU");          // ulong
```

### Arithmetic & operator precedence

Full C# precedence and parenthesization are honored across all numeric types (`byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double`, `decimal`, `char`):

```csharp
new CSharpParser().Evaluate<int>("5-4*2");       // -3
new CSharpParser().Evaluate<int>("(5+4)/2");     // 4
```

Unary operators, bitwise operators and shifts are supported too:

```csharp
new CSharpParser().Evaluate<int>("-(3-2)");      // -1   (unary minus)
new CSharpParser().Evaluate<bool>("!!true");     // true (logical not)
new CSharpParser().Evaluate<int>("~2");          // ~2   (bitwise complement)
new CSharpParser().Evaluate<int>("5+~2");        // bitwise, &, |, ^, <<, >> all supported
```

### Comparisons, booleans & nullable lifting

```csharp
parser.Evaluate<bool>("order.Total >= 100");                 // threshold check
parser.Evaluate<bool>("order.Status == \"paid\" && order.Total > 0");  // && and || short-circuit
parser.Evaluate<int?>("order.Total + surcharge");            // nullable operands are lifted...
parser.Evaluate<int?>("order.Total + unknownFee");           // ...and null propagates if an operand is null
```

Comparisons defer to the actual .NET operators on your types, so `DateTime`, `TimeSpan` and any type with overloaded operators just work:

```csharp
context.Set("dueDate",  DateTime.Today.AddDays(30));
context.Set("shipDate", order.ShippedDate);

parser.Evaluate<bool>("shipDate < dueDate");                  // shipped on time?
parser.Evaluate<double>("(dueDate - shipDate).TotalDays");    // days of slack
```

### Ternary, coalescing and null-aware operators

```csharp
// Tiered shipping label via chained ternary:
parser.Evaluate<string>("order.Total >= 100 ? \"free\" : order.Total >= 50 ? \"reduced\" : \"standard\"");

// Null-coalescing ?? — fall back when a value is missing:
parser.Evaluate<string>("customer.Country ?? \"unknown\"");

// "default value" operator ?: — use the fallback when the left side is falsy/empty:
parser.Evaluate<string>("customer.Name ?: \"Valued customer\"");

// "value-or-null" operator :: — the value only when the condition holds, else null:
parser.EvaluateToObject("order.IsPaid :: order.Total");   // Total if paid, otherwise null
```

### Type operators: `is`, `as`, `typeof`, casts

```csharp
new CSharpParser().Evaluate<bool>("\"x\" is string");    // true
new CSharpParser().Evaluate<string>("5 as string");      // null
new CSharpParser().Evaluate<Type>("typeof(int)");        // typeof(int) — incl. all built-in aliases
new CSharpParser().EvaluateToObject("(long)5");          // 5L — explicit casts, incl. custom implicit conversions
```

### Member access, methods, indexers & construction

```csharp
parser.Evaluate<string>("order.Customer.Name.ToUpper()");  // member chains + real .NET methods
parser.Evaluate<int>("order.Customer.Name.Length");        // property access
parser.Evaluate<DateTime>("DateTime.Today");               // static members of a registered type

parser.Evaluate<string>("order.Lines[0].Product");         // array element access
parser.Evaluate<int>("customer.Orders[0].Id");             // IList element access
parser.Evaluate<decimal>("priceMatrix[zone, weight]");     // multi-dimensional arrays / multi-arg indexers

parser.Evaluate<DateTime>("new DateTime(2026, 1, 1)");     // object construction with 'new'
parser.Evaluate<int>("new DateTime(2026, 1, 1).Month");    // and immediate member access
```

The `?.` null-conditional member operator is also recognized, so `order.Customer?.Name` won't throw when `Customer` is null.

### Delegates & registered functions as callables

Any delegate placed in the context becomes callable — a clean way to expose trusted helpers to rule authors. A **list of delegates** acts as an overload set resolved by argument count/type:

```csharp
// Expose a rounding helper to expressions:
context.Set("round", new Func<decimal, decimal>(d => Math.Round(d, 2)));
parser.Evaluate<decimal>("round(order.Total * 0.9m)");

// One name, several overloads:
context.Set("discount", new Delegate[]
{
    new Func<decimal, decimal>(total => total * 0.95m),                        // flat 5% off
    new Func<decimal, bool, decimal>((total, premium) => total * (premium ? 0.90m : 0.98m))
});
parser.Evaluate<decimal>("discount(order.Total)");         // picks the 1-arg overload
parser.Evaluate<decimal>("discount(order.Total, true)");   // picks the 2-arg overload
```

### Numeric ranges

A distinctive feature: inclusive/exclusive integer ranges that evaluate to `IEnumerable<int>` (or `<long>`), and can run forward or backward — handy for generating months, installments, page numbers, or driving loops.

| Operator | `1 op 5` yields | Meaning |
| --- | --- | --- |
| `...`   | `1,2,3,4,5` | both endpoints inclusive |
| `>...`  | `2,3,4,5`   | exclude the start |
| `...<`  | `1,2,3,4`   | exclude the end |
| `>...<` | `2,3,4`     | exclude both |

```csharp
new CSharpParser().Evaluate<IEnumerable<int>>("1...12");        // month numbers Jan..Dec
new CSharpParser().Evaluate<IEnumerable<int>>("10>...<20").Sum(); // 11+12+...+19
new CSharpParser().Evaluate<IEnumerable<int>>("5...1");         // reversed: 5,4,3,2,1 (a countdown)
```

Ranges are also what power `foreach` loops and template loops (`[0...<n]`, `[5...1]`, `(1...5)`).

---

## The scripting language

Use **`CScriptParser`** (which is `CSharpParser` with scripting enabled) to run multi-statement scripts. On top of everything in the expression language, scripts add:

- `;`-separated statements and `{ ... }` blocks
- `if` / `else` / `else if`
- `while` loops
- `foreach (x in sequence)` loops
- `break`
- `return` (with a value)
- `function name(args) { ... }` definitions, including recursion
- variable assignment (subject to the context's `AssignmentPermissions`)

```csharp
var parser = new CScriptParser();
```

In the examples below, `ctx` is a context that holds `order`/`customer` and an `Action<object>` registered as `log`, created with `AssignmentPermissions.All` so scripts may declare variables.

### Control flow

```csharp
// foreach over an order's lines, stopping at the first big-ticket item:
parser.Evaluate(@"
    foreach (line in order.Lines)
    {
        log(line.Product);
        if (line.LineTotal > 1000) break;
    }
", ctx);

// while loop — count how many lines fit within a budget:
parser.Evaluate("i = 0; spent = 0.0m; while (spent + order.Lines[i].LineTotal <= budget) { spent = spent + order.Lines[i].LineTotal; i = i + 1; }", ctx);

// if / else if / else — assign a shipping tier:
parser.Evaluate("if (order.Total >= 100) tier = \"free\"; else if (order.Total >= 50) tier = \"reduced\"; else tier = \"standard\";", ctx);
```

### Return values

`return` stops execution and produces the script's value — even from inside a block or loop. It's ideal for guard clauses:

```csharp
string status = parser.Evaluate<string>(@"
    if (customer.IsBlocked) return ""blocked"";
    if (order.Total <= 0)   return ""empty"";
    return ""ok"";
", ctx);
```

### User-defined functions

```csharp
decimal gross = parser.Evaluate<decimal>(@"
    // A reusable VAT helper:
    function withTax(amount, ratePct) { return amount * (1 + ratePct / 100.0m); }

    return withTax(order.Total, 21);   // apply 21% VAT
", ctx);
```

Functions can be defined in one `Evaluate` call and reused in a later one, as long as they share the same parser/context — handy for registering a script "library" up front:

```csharp
parser.Evaluate("function shippingFor(total) { return total >= 50 ? 0.0m : 4.95m; }");

parser.Evaluate<decimal>("shippingFor(order.Total)");   // reuses the definition above
```

> **Note:** Assignment (`x = ...`) requires the context to grant permission — see [AssignmentPermissions](#controlling-assignment). Scripts typically use `ParserContextBehavior.Easy` and `AssignmentPermissions.All`.

---

## Contexts: variables, types, functions & objects

A **context** is the environment an expression runs in. `ParserContext` is the workhorse; `FlexContext` is a convenience subclass; `DynamicObject` is a flexible object aggregator.

### Registering values, types and functions

```csharp
var context = new ParserContext();

context.Set("order", order);                    // a variable holding your domain object
context.Set("taxRate", 21, typeof(int));        // variable with explicit static type
context["currency"] = "EUR";                    // indexer shorthand

context.AddType("Math", typeof(Math));          // make a type available: "Math.Max(...)" / "Math.Round(...)"
context.AddType("DateTime", typeof(DateTime));  // enables "new DateTime(...)" and "DateTime.Today"

context.AddFunction("Max", typeof(Math), "Max");        // expose a static method as a bare function
context.AddFunction("fmt", typeof(string), "Format");   // "fmt(...)" -> string.Format(...)
```

The explicit-type overload matters for nullable and interface-typed values, so operators still behave correctly:

```csharp
context.Set("shippedDate", order.ShippedDate, typeof(DateTime?));  // may be null
context.Set("couponCode",  null, typeof(string));
```

### Root objects — evaluate directly against a POCO

Set a **root object** and its members become top-level names (no prefix needed) — great when each evaluation targets a single entity:

```csharp
var context = new ParserContext(customer);       // customer is any object
parser.Evaluate<string>("Name", context);        // reads customer.Name
parser.Evaluate<bool>("IsPremium && !IsBlocked", context);
parser.Evaluate<int>("Orders.Count", context);
```

### `FlexContext` — the "just make it work" context

`FlexContext` is a `ParserContext` preconfigured with `ParserContextBehavior.Easy` (null-safe member access + truthy/falsy coercion). It also has handy constructors for wrapping objects, dictionaries, anonymous types, and JSON:

```csharp
var ctx  = new FlexContext(new { order, customer });   // expose several objects at once
var ctx2 = new FlexContext(jsonPayload);               // wrap parsed JSON as the root
```

### `DynamicObject` — aggregate many objects into one

`DynamicObject` merges dictionary entries and the reflected members of one or more backing objects into a single addressable object. It's ideal for building a view-model / view-data bag for a page or an email from several sources:

```csharp
var viewData = new DynamicObject(customer, order);  // members of both objects, side by side
viewData["Name"];      // from customer
viewData["Total"];     // from order
viewData["Discount"] = 0.1m;            // add ad-hoc entries
viewData.Apply(loyaltyInfo);            // merge in more objects later

var context = new ParserContext(viewData);
parser.Evaluate<string>("Name + \" owes \" + Total", context);  // resolves across all backing objects
```

### Local (nested) scopes

`CreateLocal()` produces a child context. Reads fall through to the parent; `SetLocal` shadows a name only in the child; `Set` writes through to the parent if the name already exists there. This is exactly how loops, macros, and included templates get their own variable scope.

```csharp
var ctx = new ParserContext();
ctx["taxRate"] = 21;

var local = ctx.CreateLocal();
local.SetLocal("taxRate", 6);   // reduced rate, only inside this scope

ctx.Get("taxRate",   out var global, out _);   // 21  (parent unchanged)
local.Get("taxRate", out var scoped, out _);   // 6   (shadowed in child)
```

### Truthy/falsy behavior (`ParserContextBehavior`)

By default, only real `bool` values are valid in boolean positions — anything else throws. You opt into looser, "scripting-friendly" coercions with `[Flags] ParserContextBehavior`:

| Flag | Effect |
| --- | --- |
| `Default` | Strict: only `bool` is boolean; non-bool throws. |
| `NullIsFalse` / `NotNullIsTrue` | `null` ⇒ false / non-null ⇒ true. |
| `ZeroIsFalse` / `NotZeroIsTrue` | numeric zero ⇒ false / non-zero ⇒ true. |
| `EmptyStringIsFalse` / `NonEmptyStringIsTrue` | string emptiness ⇒ boolean. |
| `EmptyCollectionIsFalse` | empty `ICollection`/`IEnumerable` ⇒ false. |
| `Falsy` | all of the coercions above combined. |
| `ReturnNullWhenNullReference` | null-safe member access (like `?.`) instead of throwing. |
| `Easy` | `Falsy` + `ReturnNullWhenNullReference` — the recommended scripting default. |
| `CaseInsensitiveVariables` | variable names are case-insensitive. |
| `CaseInsensitiveMembers` | member/property lookups are case-insensitive. |

```csharp
var strict = new ParserContext(ParserContextBehavior.Default);
// Using a string where a bool is expected throws under the default behavior.

// With coercion, an empty string counts as "false" — so a missing coupon code is falsy:
var easy = new ParserContext(ParserContextBehavior.EmptyStringIsFalse);
easy.Set("couponCode", "");
new CSharpParser().Evaluate<bool>("!!couponCode", easy);   // false — empty string is treated as false

// Case-insensitive names/members are handy when rule authors don't match your casing exactly:
var ci = new ParserContext(
    ParserContextBehavior.Easy |
    ParserContextBehavior.CaseInsensitiveVariables |
    ParserContextBehavior.CaseInsensitiveMembers);
ci["customer"] = customer;
parser.Evaluate<string>("CUSTOMER.name", ci);   // resolves 'customer.Name'
```

### Controlling assignment

Assignment inside expressions/scripts is disabled unless explicitly allowed via `[Flags] AssignmentPermissions`:

| Flag | Allows |
| --- | --- |
| `None` | nothing (default). |
| `NewVariable` | creating new variables. |
| `ExistingVariable` | reassigning existing variables. |
| `Variable` | both of the above. |
| `Property` | assigning to object properties/fields. |
| `Indexer` | assigning through indexers. |
| `All` | everything. |

```csharp
var ctx = new ParserContext { AssignmentPermissions = AssignmentPermissions.NewVariable };
parser.Evaluate<decimal>("subtotal = 42.50m", ctx);       // ok, creates 'subtotal'

ctx.AssignmentPermissions = AssignmentPermissions.Variable;
parser.Evaluate<decimal>("subtotal = shipping = 4.95m", ctx); // chained assignment

ctx.AssignmentPermissions = AssignmentPermissions.Property;
parser.Evaluate<string>("order.Status = \"shipped\"", ctx);   // writes back to your object

// With AssignmentPermissions.None (the default), "order.Status = ..." throws IllegalAssignmentException —
// so read-only rule evaluation can never mutate your data by accident.
```

### String comparison & formatting

Contexts also control how string `==`/`!=` behave and how numbers/dates are formatted:

```csharp
// Compare country codes case-insensitively:
var ctx = new ParserContext { StringComparison = StringComparison.InvariantCultureIgnoreCase };
parser.Evaluate<bool>("customer.Country == \"be\"", ctx);   // true even if stored as "BE"

ctx.FormatProvider = CultureInfo.GetCultureInfo("nl-BE");   // used by template format specifiers and Format(...)
```

---

## Template rendering

`TemplateParser<TConfig>` renders a text template by evaluating embedded expressions, loops, conditionals and macros. Pick a `TConfig` for the syntax you want; all of them use the same underlying expression/scripting engine and the same `ParserContext`.

### Available syntaxes

| Config | Expression | Loop | Conditional | Notes |
| --- | --- | --- | --- | --- |
| `DoubleCurly` | `{{ expr }}` | `{{foreach x in list}}…{{end}}` | `{{if c}}…{{else}}…{{end}}` | General-purpose. |
| `HtmlDoubleCurly` | `<!--{{ expr }}-->` (also `{{ expr }}`) | `<!--{{foreach …}}-->…<!--{{end}}-->` | `<!--{{if …}}-->…<!--{{end}}-->` | Directives hidden in HTML comments, so the raw template is still valid HTML. |
| `Velocity` | `$var`, `${ expr }` | `#foreach(x in list)…#end` (or `#{foreach}(…)…#{end}`) | — | Apache-Velocity-like. |
| `Xml` | `$var`, `${ expr }` | `<foreach var='x' in='list'>…</foreach>` | `<if condition='…'>…<else/>…</if>` | Output is XML-escaped automatically. |

```csharp
var curly    = new TemplateParser<DoubleCurly>();
var html     = new TemplateParser<HtmlDoubleCurly>();
var velocity = new TemplateParser<Velocity>();
var xml      = new TemplateParser<Xml>();
```

You can also give a template parser its own subclass name for convenience:

```csharp
public class CurlyTemplateParser : TemplateParser<DoubleCurly> { }
```

### Expressions & formatting

Everything valid in the expression language is valid inside a template placeholder. The backtick introduces a .NET format string:

```csharp
curly.Render("Balance due: {{order.Total ` 0.00}}", context);          // "Balance due: 20.50"
velocity.Render("Hi $customer.Name, your total is ${order.Total ` 0.00}", context);
```

`Velocity`/`Xml` `$name` "smart expressions" greedily consume a member/call chain, so `$customer.Name!` renders the name immediately followed by `!`.

### Loops

```csharp
// {{ }} syntax — one receipt line per item:
curly.Render("{{foreach line in order.Lines}}{{line.Quantity}} x {{line.Product}}\n{{end}}", context);

// numeric ranges as the sequence — e.g. a star rating or pagination:
curly.Render("{{ foreach n in (1...5) }}*{{end}}", context);      // "*****"
curly.Render("{{ foreach n in [5...1] }}{{n}} {{end}}", context); // "5 4 3 2 1 "  (reverse / countdown)

// Velocity & XML equivalents:
velocity.Render("#foreach(line in order.Lines)${line.Product}, #end", context);
xml.Render("<foreach var='line' in='order.Lines'>${line.Product}, </foreach>", context);
```

Inside a loop, iteration metadata variables are injected automatically for the iterator (here `line`):
`line@row` (1-based), `line@index` (0-based), `line@odd`, `line@even`, and the string helpers `line@oddeven` / `line@ODDEVEN` / `line@OddEven` — handy for zebra-striping table rows.

### Conditionals

```csharp
curly.Render("{{if order.Total >= 50}}Free shipping!{{else}}Add more to qualify.{{end}}", context);
xml.Render("<if condition='customer.IsPremium'>VIP<else/>Standard</if>", context);
```

### Macros

Define reusable fragments and call them with named `@parameters`. Macros may call other macros and are resolved regardless of definition order — perfect for shared snippets like a money formatter or a line-item row:

```csharp
// A reusable "money" macro, invoked with a named @amount parameter:
curly.Render(
    "{{macro money}}EUR {{amount ` 0.00}}{{end}}" +
    "Subtotal: {{call money @amount=order.Total}}",
    context);
// "Subtotal: EUR 20.50"   (for order.Total == 20.5)

// One macro calling another:
curly.Render(
    "{{macro row}}{{product}}: {{call money @amount=price}}{{end}}" +
    "{{macro money}}EUR {{amount ` 0.00}}{{end}}" +
    "{{call row @product=\"Widget\" @price=9.5}}",
    context);
// "Widget: EUR 9.50"
```

### Files, includes and reuse

- `Parse(input)` returns a reusable `CompiledTemplate`; `Render(compiled, context)` runs it repeatedly.
- `ParseFile` / `RenderFile` plus template directives for including/parsing other files work through an `IFileResolver` you supply on the config (`Config.FileResolver`), so file access stays under your control and remains testable.

Each render runs in its own local scope with `AssignmentPermissions.Variable` granted, so templates can define working variables without mutating your outer context.

---

## Building expression trees by hand

If you'd rather construct expressions programmatically (no parsing), use the `Exp` factory to build a tree and evaluate it directly. This is handy for generating expressions from a UI or another DSL.

```csharp
using Iridium.Script;

IParserContext context = new ParserContext();

// unitPrice * quantity, assembled from parts:
var lineTotal = new ExpressionWithContext(context, Exp.Multiply(Exp.Value(9.99m), Exp.Value(3)));
lineTotal.Evaluate<decimal>();   // 29.97
lineTotal.Evaluate().Type;       // typeof(decimal)

// Mixed types promote just like the parser would:
var expr2 = new ExpressionWithContext(context, Exp.Add(Exp.Value(10), Exp.Value(2.5)));
expr2.Evaluate().Value;   // 12.5   (int + double => double)

// Arbitrary operators by symbol — e.g. a threshold check:
var overThreshold = new ExpressionWithContext(context, Exp.Op(">=", Exp.Value(120m), Exp.Value(100)));
overThreshold.Evaluate<bool>();   // true
```

`Exp` provides factories for `Add`/`Subtract`/`Multiply`/`Divide`, `Value`, `Op`, `AndAlso`/`OrElse`, `Equal`, `Field`, `As`, `Assign`, `BitwiseComplement`, `Call`, `Coalesce`, `Conditional`, `DefaultValue`, and more.

---

## Error handling

Parsing and evaluation raise specific exception types so you can react precisely:

**Expression/scripting** (`Iridium.Script`):

| Exception | Raised when |
| --- | --- |
| `LexerException` | the input can't be tokenized / has invalid syntax. |
| `ParserException` | the token stream can't be assembled into a valid expression. |
| `ExpressionEvaluationException` | an error occurs while evaluating (e.g. a range endpoint isn't `int`/`long`). |
| `IllegalAssignmentException` | an assignment isn't permitted by the context's `AssignmentPermissions`. |
| `IllegalOperandsException` | an operator is applied to incompatible operand types. |
| `BadArgumentException` | a method/function is called with unmatched arguments. |
| `UnknownPropertyException` | a member can't be resolved on the target. |
| `LiteralException` | a literal is malformed. |

Under `ParserContextBehavior.Default`, using a non-`bool` in a boolean position throws `NullReferenceException` (for `null`) or `ArgumentException` (for other types); enable the `Falsy`/`Easy` behaviors to coerce instead.

**Templates** (`Iridium.Script`): `TemplateParsingException` (during `Parse`) and `TemplateRenderingException` (during `Render`) wrap the underlying cause in `InnerException`.

---

## How it works (architecture)

The pipeline is a classic, fully-managed compiler front-end followed by a tree-walking evaluator:

```
source string
   │
   ▼
Tokenizer                (composable ITokenMatcher rules: CharMatcher, StringMatcher,
   │  tokens              RegexMatcher, VariableMatcher, literal matchers, …)
   ▼
ExpressionCompiler       (shunting-yard / RPN with per-operator precedence & associativity;
   │  expression tree      recognizes scripting constructs: if/while/foreach/function/return)
   ▼
Expression tree          (AddExpression, CallExpression, FieldExpression, ConditionalExpression,
   │                        RangeExpression, ForEachExpression, IfExpression, …)
   ▼
Evaluate(IParserContext) → ValueExpression / IValueWithType  (value + static type)
```

Key design points:

- **No `Reflection.Emit`.** Member access, method calls and operators are resolved at evaluation time via reflection and a `SmartBinder` that performs C#-like overload resolution and argument conversion (leaning on `iridium.convert`). This is why it runs under AOT and on iOS/mobile.
- **Types flow through evaluation.** Every node yields an `IValueWithType`, so numeric promotion, nullable lifting, and operator overload selection behave like real C#.
- **Pluggable tokenizers.** `CSharpTokenizer` builds the C# operator set (and enables the scripting keywords when constructed with `allowScripting: true`, which is what `CScriptParser` does). Template tokenizers (`DoubleCurlyTokenizer`, `VelocityTokenizer`, `XmlTokenizer`, `HtmlDoubleCurlyTokenizer`) reuse the same matcher framework.
- **Extensible template engine.** `TemplateParserConfig` exposes `virtual` hooks (`OnEvalExpression`, `OnEvalIf`, `OnEvalForeach`, `OnEvalText`, `OnEvalMacroCall`, …). The `Xml` config, for example, overrides `OnEvalExpression` purely to XML-escape output.

### Operator precedence (high → low)

Derived from `CSharpTokenizer`:

| Level | Operators |
| --- | --- |
| Member access | `.`  `?.` |
| Unary / cast | `!`  `-`  `~`  `(cast)` |
| Multiplicative | `*`  `/`  `%` |
| Additive | `+`  `-` |
| Shift | `<<`  `>>` |
| Relational / type-test | `<`  `<=`  `>`  `>=`  `is`  `as` |
| Equality | `==`  `!=` |
| Bitwise AND/OR | `&`  `\|` |
| Bitwise XOR | `^` |
| Conditional AND | `&&` |
| Conditional OR | `\|\|` |
| Null/default | `??`  `?:`  `::` |
| Ternary | `? :` |
| Assignment | `=` |
| `in` (scripting) | `in` |
| Range | `...`  `...<`  `>...`  `>...<` |

---

## API cheat-sheet

**Parsers** (`Iridium.Script.CSharp`)

```csharp
new CSharpParser()                 // expression evaluation
new CSharpParser(allowScripting)   // opt into scripting keywords
new CScriptParser()                // = CSharpParser(true)
CSharpParser.Default               // shared instance
```

**Evaluate (all have a `..., IParserContext context` overload; without it the parser's `DefaultContext` is used)**

```csharp
T        Evaluate<T>(string s)
IValueWithType Evaluate(string s)
object   Evaluate(string s, out Type type)
object   EvaluateToObject(string s)
Expression Parse(string s)                    // parse once, evaluate many
ExpressionWithContext ParseWithContext(string s[, context])
```

**Contexts** (`Iridium.Script`)

```csharp
new ParserContext([behavior])
new ParserContext(rootObject[, behavior])
new FlexContext(...)               // ParserContext with Easy behavior
new DynamicObject(params object[]) // aggregate objects + ad-hoc entries

context.Set(name, value[, type]);  context[name] = value;
context.SetLocal(name, value);
context.AddType(name, type);
context.AddFunction(name, type, methodName[, target]);
context.CreateLocal();
context.AssignmentPermissions = AssignmentPermissions.All;
context.StringComparison = StringComparison.OrdinalIgnoreCase;
context.FormatProvider  = CultureInfo.InvariantCulture;
```

**Templates** (`Iridium.Script`)

```csharp
var t = new TemplateParser<DoubleCurly>();   // or HtmlDoubleCurly / Velocity / Xml
string  output   = t.Render(inputString, context);
CompiledTemplate ct = t.Parse(inputString);  // reusable
string  output2  = t.Render(ct, context);
string  fromFile = t.RenderFile(fileName, context);  // via Config.FileResolver
```

**Expression trees** (`Iridium.Script`)

```csharp
var e = new ExpressionWithContext(context, Exp.Add(Exp.Value(1), Exp.Value(2)));
e.Evaluate();          // IValueWithType
e.Evaluate<int>();     // 3
e.EvaluateToObject();  // (object)3
```

---

*Iridium.Script — Copyright © 2008–2018 Philippe Leybaert. Released under the MIT license.*
