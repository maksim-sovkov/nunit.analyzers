using Gu.Roslyn.Asserts;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Analyzers.Constants;
using NUnit.Analyzers.UseAssertMultiple;
using NUnit.Framework;

namespace NUnit.Analyzers.Tests.UseAssertMultiple
{
    [TestFixture]
    public sealed class UseAssertMultipleAnalyzerTests
    {
        private readonly DiagnosticAnalyzer analyzer = new UseAssertMultipleAnalyzer();
        private readonly ExpectedDiagnostic diagnostic = ExpectedDiagnostic.Create(AnalyzerIdentifiers.UseAssertMultiple);

        [Test]
        public void AnalyzeWhenMultipleIsUsed()
        {
            var testCode = TestUtility.WrapMethodInClassNamespaceAndAddUsings(@"
        public void Test()
        {
            Assert.Multiple(() =>
            {
                Assert.That(true, Is.True);
                Assert.That(false, Is.False);
            });
        }");
            RoslynAssert.Valid(this.analyzer, testCode);
        }

#if NUNIT4
        [Test]
        public void AnalyzeWhenMultipleAsyncIsUsed()
        {
            var testCode = TestUtility.WrapMethodInClassNamespaceAndAddUsings(@"
        public async Task Test()
        {
            await Assert.MultipleAsync(async () =>
            {
                Assert.That(await Get1(), Is.Not.Null);
                Assert.That(await Get2(), Is.Not.Null);
            });

            static Task<string?> Get1() => Task.FromResult(default(string));
            static Task<string?> Get2() => Task.FromResult(default(string));
        }");
            RoslynAssert.Valid(this.analyzer, testCode);
        }

        [Test]
        public void AnalyzeWhenMultipleScopeDeclarationIsUsed()
        {
            var testCode = TestUtility.WrapMethodInClassNamespaceAndAddUsings(@"
        public void Test()
        {
            using IDisposable disposable = Assert.EnterMultipleScope();

            Assert.That(true, Is.True);
            Assert.That(false, Is.False);
        }");
            RoslynAssert.Valid(this.analyzer, testCode);
        }

        [Test]
        public void AnalyzeWhenMultipleScopeDeclarationIsUsedAfterPossibleLocation()
        {
            var testCode = TestUtility.WrapMethodInClassNamespaceAndAddUsings(@"
        public void Test()
        {
            ↓Assert.That(true, Is.True);
            Assert.That(false, Is.False);

            using IDisposable disposable = Assert.EnterMultipleScope();

            Assert.That(true, Is.True);
            Assert.That(false, Is.False);
        }");
            RoslynAssert.Diagnostics(this.analyzer, testCode);
        }

        [Test]
        public void AnalyzeWhenMultipleScopeStatementIsUsed()
        {
            var testCode = TestUtility.WrapMethodInClassNamespaceAndAddUsings(@"
        public void Test()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(true, Is.True);
                Assert.That(false, Is.False);
            }
        }");
            RoslynAssert.Valid(this.analyzer, testCode);
        }
#endif

        [Test]
        public void AnalyzeWhenDependent()
        {
            var testCode = TestUtility.WrapMethodInClassNamespaceAndAddUsings(@"
        public void Test()
        {
            byte[] instance = new byte[] { 0xBB };
            Assert.That(instance, Is.Not.Null);
            Assert.That(instance, Has.Length.EqualTo(1));
            Assert.That(instance.Length, Is.EqualTo(1));
            Assert.That(instance[0], Is.EqualTo(0xBB));
        }");
            RoslynAssert.Valid(this.analyzer, testCode);
        }

        [Test]
        public void AnalyzeWhenPartlyDependent()
        {
            var testCode = TestUtility.WrapMethodInClassNamespaceAndAddUsings(@"
        public void Test()
        {
            var configuration = new Configuration();
            Assert.That(configuration, Is.Not.Null);
            ↓Assert.That(configuration.Value1, Is.EqualTo(0));
            Assert.That(configuration.Value2, Is.EqualTo(0.0));
            Assert.That(configuration.Value11, Is.EqualTo(string.Empty));
        }

        private sealed class Configuration
        {
            public int Value1 { get; set; }
            public double Value2 { get; set; }
            public string Value11 { get; set; } = string.Empty;
        }");
            RoslynAssert.Diagnostics(this.analyzer, this.diagnostic, testCode);
        }

        [Test]
        public void AnalyzeWhenIndependent()
        {
            var testCode = TestUtility.WrapMethodInClassNamespaceAndAddUsings(@"
        public void Test()
        {
            ↓Assert.That(true, Is.True);
            Assert.That(false, Is.False);
        }");
            RoslynAssert.Diagnostics(this.analyzer, this.diagnostic, testCode);
        }

        [Test]
        public void AnalyzeWhenNoParameterIsUsedInFirstCall()
        {
            var testCode = TestUtility.WrapMethodInClassNamespaceAndAddUsings(@"
        public void Test()
        {
            Assert.Fail();
            Assert.That(string.Empty, Has.Count.EqualTo(1));
        }");
            RoslynAssert.Valid(this.analyzer, testCode);
        }

        [Test]
        public void AnalyzeWhenNoParameterIsUsedInSecondCall()
        {
            var testCode = TestUtility.WrapMethodInClassNamespaceAndAddUsings(@"
        public void Test()
        {
            Assert.That(string.Empty, Has.Count.EqualTo(1));
            Assert.Fail();
        }");
            RoslynAssert.Valid(this.analyzer, testCode);
        }

        [Test]
        public void AnalyzeWhenUsingAnonymousLambda()
        {
            var testCode = TestUtility.WrapMethodInClassNamespaceAndAddUsings(@"
        public void Test()
        {
            object? actualDeserialized = null;

            Assert.That(() => actualDeserialized = Calculate(), Throws.Nothing);
            Assert.That(actualDeserialized, Is.Not.Null);

            static object? Calculate() => new object();
        }");

            RoslynAssert.Valid(this.analyzer, testCode);
        }

        [Test]
        public void AnalyzeWhenUsingTestDelegate()
        {
            var testCode = TestUtility.WrapMethodInClassNamespaceAndAddUsings(@"
        public void Test()
        {
            object? actualDeserialized = null;

            Assert.That(Calculate, Throws.Nothing);
            Assert.That(actualDeserialized, Is.Not.Null);

            void Calculate() => actualDeserialized = new object();
        }");

            RoslynAssert.Valid(this.analyzer, testCode);
        }
    }
}
