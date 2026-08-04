//=============================================================================
// Iridium Script - Portable .NET Productivity Library 
//
// Tests for MemberAccessPolicy: a script cannot reach the reflection system,
// but ordinary members of the host's own objects stay available.
//=============================================================================

using System;
using System.Collections.Generic;
using System.Reflection;
using Iridium.Script.CSharp;
using NUnit.Framework;

namespace Iridium.Script.Test
{
    [TestFixture]
    public class MemberAccessPolicyTests
    {
        private class Customer
        {
            public string Name { get; set; } = "Alice";
            public int Age { get; set; } = 30;
            public Customer Manager { get; set; }
            public List<string> Tags { get; } = ["vip"];

            public string Describe() => Name + "/" + Age;
        }

        // A host type that carelessly exposes reflection objects.
        private class Leaky
        {
            public Type TypeProperty => typeof(int);
            public Type GetTypeMethod() => typeof(int);
            public Type[] TypeArray => [typeof(int)];
            public IEnumerable<Type> TypeSequence => [typeof(int)];
            public Assembly AssemblyProperty => typeof(int).Assembly;
            public Action Callback => () => { };
            public void TakesType(Type type) { }
        }

        private ParserContext NewContext()
        {
            var context = new ParserContext { AssignmentPermissions = AssignmentPermissions.All };

            context.Set("customer", new Customer());
            context.Set("leaky", new Leaky());

            return context;
        }

        private object Run(string script) => new CScriptParser().EvaluateToObject(script, NewContext());

        private T Run<T>(string script) => new CScriptParser().Evaluate<T>(script, NewContext());

        private void AssertBlocked(string script)
        {
            var ex = Assert.Throws<ExpressionEvaluationException>(() => Run(script), "Expected script to be blocked: " + script);

            Assert.That(ex.Message, Does.Contain("not allowed"));
        }

        // ---------------------------------------------------------------------
        // The policy itself
        // ---------------------------------------------------------------------

        [Test]
        public void OrdinaryMembersAreSafe()
        {
            Assert.That(MemberAccessPolicy.IsSafe(typeof(Customer).GetProperty("Name")), Is.True);
            Assert.That(MemberAccessPolicy.IsSafe(typeof(Customer).GetProperty("Age")), Is.True);
            Assert.That(MemberAccessPolicy.IsSafe(typeof(Customer).GetProperty("Manager")), Is.True);
            Assert.That(MemberAccessPolicy.IsSafe(typeof(Customer).GetProperty("Tags")), Is.True);
            Assert.That(MemberAccessPolicy.IsSafe(typeof(Customer).GetMethod("Describe")), Is.True);
            Assert.That(MemberAccessPolicy.IsSafe(typeof(string).GetProperty("Length")), Is.True);
            Assert.That(MemberAccessPolicy.IsSafe(typeof(string).GetMethod("ToUpper", Type.EmptyTypes)), Is.True);
        }

        [Test]
        public void GetTypeIsUnsafeOnAnyObject()
        {
            Assert.That(MemberAccessPolicy.IsSafe(typeof(object).GetMethod("GetType")), Is.False);
            Assert.That(MemberAccessPolicy.IsSafe(typeof(Customer).GetMethod("GetType")), Is.False);
            Assert.That(MemberAccessPolicy.IsSafe(typeof(string).GetMethod("GetType")), Is.False);
        }

        [Test]
        public void EveryMemberOfTheReflectionModelIsUnsafe()
        {
            Assert.That(MemberAccessPolicy.IsSafe(typeof(Type).GetProperty("Assembly")), Is.False);
            Assert.That(MemberAccessPolicy.IsSafe(typeof(Type).GetProperty("Module")), Is.False);
            Assert.That(MemberAccessPolicy.IsSafe(typeof(Type).GetProperty("BaseType")), Is.False);
            Assert.That(MemberAccessPolicy.IsSafe(typeof(Type).GetProperty("Name")), Is.False);
            Assert.That(MemberAccessPolicy.IsSafe(typeof(Type).GetMethod("GetConstructors", Type.EmptyTypes)), Is.False);
            Assert.That(MemberAccessPolicy.IsSafe(typeof(Assembly).GetMethod("GetType", [typeof(string)])), Is.False);
            Assert.That(MemberAccessPolicy.IsSafe(typeof(Module).GetMethod("GetType", [typeof(string)])), Is.False);
            Assert.That(MemberAccessPolicy.IsSafe(typeof(MethodBase).GetMethod("Invoke", [typeof(object), typeof(object[])])), Is.False);
        }

        [Test]
        public void MembersHandingOutReflectionObjectsAreUnsafe()
        {
            Assert.That(MemberAccessPolicy.IsSafe(typeof(Leaky).GetProperty("TypeProperty")), Is.False);
            Assert.That(MemberAccessPolicy.IsSafe(typeof(Leaky).GetMethod("GetTypeMethod")), Is.False);
            Assert.That(MemberAccessPolicy.IsSafe(typeof(Leaky).GetProperty("TypeArray")), Is.False);
            Assert.That(MemberAccessPolicy.IsSafe(typeof(Leaky).GetProperty("TypeSequence")), Is.False);
            Assert.That(MemberAccessPolicy.IsSafe(typeof(Leaky).GetProperty("AssemblyProperty")), Is.False);
            Assert.That(MemberAccessPolicy.IsSafe(typeof(Leaky).GetProperty("Callback")), Is.False);
            Assert.That(MemberAccessPolicy.IsSafe(typeof(Leaky).GetMethod("TakesType")), Is.False);
        }

        [Test]
        public void NullIsNotSafe()
        {
            Assert.That(MemberAccessPolicy.IsSafe(null), Is.False);
        }

        // ---------------------------------------------------------------------
        // The escape routes, end to end
        // ---------------------------------------------------------------------

        [Test]
        public void ScriptCannotCallGetType()
        {
            AssertBlocked("customer.GetType()");
        }

        [Test]
        public void ScriptCannotReachAssemblyOrModule()
        {
            // typeof() is part of the language, so a Type can still be produced - but it
            // is inert, because every route onwards from it is closed.
            AssertBlocked("typeof(int).Assembly");
            AssertBlocked("typeof(int).Module");
            AssertBlocked("typeof(int).BaseType");
            AssertBlocked("typeof(int).Name");
        }

        [Test]
        public void ScriptCannotResolveArbitraryTypesThroughTheModule()
        {
            // The full escape that the name-based filter allowed:
            // typeof(int).Module.GetType("System.Environment") -> any type in the process.
            AssertBlocked("typeof(int).Module.GetType(\"System.Environment\")");
        }

        [Test]
        public void ScriptCannotReachReflectionThroughACarelessHostObject()
        {
            AssertBlocked("leaky.TypeProperty");
            AssertBlocked("leaky.GetTypeMethod()");
            AssertBlocked("leaky.AssemblyProperty");
            AssertBlocked("leaky.TypeArray");
        }

        [Test]
        public void ScriptCannotCallGetTypeOnARootObject()
        {
            // Bare identifiers resolve against root objects through a different code
            // path than the '.' operator, and it must be filtered too.
            var context = new ParserContext(new Customer()) { AssignmentPermissions = AssignmentPermissions.All };

            var ex = Assert.Throws<ExpressionEvaluationException>(() => new CScriptParser().EvaluateToObject("GetType()", context));

            Assert.That(ex.Message, Does.Contain("not allowed"));
        }

        [Test]
        public void MemberAccessRulesStillApplyInsideAFunctionBody()
        {
            AssertBlocked("function f() { return customer.GetType(); } return f();");
        }

        // ---------------------------------------------------------------------
        // Nothing legitimate got blocked
        // ---------------------------------------------------------------------

        [Test]
        public void OrdinaryScriptingStillWorks()
        {
            Assert.That(Run<string>("customer.Name"), Is.EqualTo("Alice"));
            Assert.That(Run<int>("customer.Age"), Is.EqualTo(30));
            Assert.That(Run<string>("customer.Name.ToUpper()"), Is.EqualTo("ALICE"));
            Assert.That(Run<int>("customer.Name.Length"), Is.EqualTo(5));
            Assert.That(Run<string>("customer.Describe()"), Is.EqualTo("Alice/30"));
            Assert.That(Run<int>("customer.Age * 2 + 1"), Is.EqualTo(61));
            Assert.That(Run<string>("customer.Tags[0]"), Is.EqualTo("vip"));
            Assert.That(Run<bool>("string.IsNullOrEmpty(customer.Name)"), Is.False);
        }

        [Test]
        public void PropertyAssignmentStillWorks()
        {
            Assert.That(Run<int>("customer.Age = 41; return customer.Age;"), Is.EqualTo(41));
        }

        [Test]
        public void MemberAccessOnRootObjectStillWorks()
        {
            var context = new ParserContext(new Customer()) { AssignmentPermissions = AssignmentPermissions.All };

            Assert.That(new CScriptParser().Evaluate<string>("Name", context), Is.EqualTo("Alice"));
            Assert.That(new CScriptParser().Evaluate<string>("Name.ToUpper()", context), Is.EqualTo("ALICE"));
        }

        [Test]
        public void HostFunctionsStillWork()
        {
            var context = new ParserContext { AssignmentPermissions = AssignmentPermissions.All };

            context.Set("twice", new Func<int, int>(n => n * 2));

            Assert.That(new CScriptParser().Evaluate<int>("twice(21)", context), Is.EqualTo(42));
        }

        // ---------------------------------------------------------------------
        // The examples given in docs/howtouse.md
        // ---------------------------------------------------------------------

        [Test]
        public void TypeOfStillProducesATypeButLeadsNowhere()
        {
            Assert.That(new CSharpParser().Evaluate<Type>("typeof(int)"), Is.EqualTo(typeof(int)));
            Assert.That(new CSharpParser().Evaluate<bool>("\"x\" is string"), Is.True);
        }

        [Test]
        public void RegisteredTypesAndConstructionStillWork()
        {
            var context = new ParserContext();

            context.AddType("DateTime", typeof(DateTime));

            Assert.That(new CSharpParser().Evaluate<DateTime>("DateTime.Today", context), Is.EqualTo(DateTime.Today));
            Assert.That(new CSharpParser().Evaluate<DateTime>("new DateTime(2026, 1, 1)", context), Is.EqualTo(new DateTime(2026, 1, 1)));
            Assert.That(new CSharpParser().Evaluate<int>("new DateTime(2026, 1, 1).Month", context), Is.EqualTo(1));
        }

        [Test]
        public void AHostFunctionCanHandOutWhatGetTypeWould()
        {
            var context = new ParserContext();

            context.Set("customer", new Customer());
            context.Set("typeName", new Func<object, string>(o => o.GetType().Name));

            Assert.That(new CSharpParser().Evaluate<string>("typeName(customer)", context), Is.EqualTo("Customer"));
        }

        [Test]
        public void AnonymousProjectionWorksAsARootObject()
        {
            var customer = new Customer();
            var context = new ParserContext(new { customer.Name, customer.Age });

            Assert.That(new CSharpParser().Evaluate<string>("Name", context), Is.EqualTo("Alice"));
            Assert.That(new CSharpParser().Evaluate<int>("Age", context), Is.EqualTo(30));
        }
    }
}
