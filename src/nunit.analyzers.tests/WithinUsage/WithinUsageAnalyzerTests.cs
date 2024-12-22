using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Gu.Roslyn.Asserts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Analyzers.Constants;
using NUnit.Analyzers.WithinUsage;
using NUnit.Framework;

namespace NUnit.Analyzers.Tests.WithinUsage
{
    public class WithinUsageAnalyzerTests
    {
        private static readonly DiagnosticAnalyzer analyzer = new WithinUsageAnalyzer();
        private static readonly ExpectedDiagnostic expectedDiagnostic =
            ExpectedDiagnostic.Create(AnalyzerIdentifiers.WithinIncompatibleTypes);

        private Settings settings;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            IEnumerable<MetadataReference> existingReferences = Settings.Default.MetadataReferences ?? Enumerable.Empty<MetadataReference>();

            this.settings = Settings.Default
                                    .WithMetadataReferences(existingReferences.Concat(MetadataReferences.Transitive(typeof(ImmutableArray<>))));
        }

#if !NUNIT4
        [TestCase(NUnitFrameworkConstants.NameOfIsEqualTo)]
#endif
        [TestCase(NUnitFrameworkConstants.NameOfIsLessThan)]
        [TestCase(NUnitFrameworkConstants.NameOfIsLessThanOrEqualTo)]
        [TestCase(NUnitFrameworkConstants.NameOfIsGreaterThan)]
        [TestCase(NUnitFrameworkConstants.NameOfIsGreaterThanOrEqualTo)]
        public void AnalyzeWhenAppliedToEqualityConstraintForStrings(string constraintName)
        {
            string testCode = TestUtility.WrapInTestMethod(
                $@"Assert.That(""1"", Is.{constraintName}(""1"").↓Within(0.1));");

            RoslynAssert.Diagnostics(analyzer, expectedDiagnostic, testCode);
        }

#if !NUNIT4
        [TestCase(NUnitFrameworkConstants.NameOfIsEqualTo)]
#endif
        [TestCase(NUnitFrameworkConstants.NameOfIsLessThan)]
        [TestCase(NUnitFrameworkConstants.NameOfIsLessThanOrEqualTo)]
        [TestCase(NUnitFrameworkConstants.NameOfIsGreaterThan)]
        [TestCase(NUnitFrameworkConstants.NameOfIsGreaterThanOrEqualTo)]
        public void AnalyzeWhenAppliedToInvertedEqualityConstraintForStrings(string constraintName)
        {
            string testCode = TestUtility.WrapInTestMethod(
                $@"Assert.That(""1"", Is.Not.{constraintName}(""1"").↓Within(0.1));");

            RoslynAssert.Diagnostics(analyzer, expectedDiagnostic, testCode);
        }

#if !NUNIT4
        [Test]
        public void AnalyzeWhenAppliedToNotEqualConstraintForStrings()
        {
            string testCode = TestUtility.WrapInTestMethod(
                @"Assert.That(""1"", Is.Not.EqualTo(""1"").↓Within(0.1));");

            RoslynAssert.Diagnostics(analyzer, expectedDiagnostic, testCode);
        }
#endif

        [Test]
        public void AnalyzeWhenAppliedToEqualityConstraintForArraysOfValidTypes()
        {
            string testCode = TestUtility.WrapInTestMethod(@"
                var a = new[] {1, 2};
                var b = new[] {1.1, 2.1};
                Assert.That(a, Is.EqualTo(b).Within(0.1));");

            RoslynAssert.Valid(analyzer, testCode);
        }

        [Test]
        public void AnalyzeWhenAppliedToEqualityConstraintForArraysOfInvalidTypes()
        {
            string testCode = TestUtility.WrapInTestMethod(@"
                var a = new[] {""Hello"", ""World""};
                var b = new[] {""Hi"", ""There""};
                Assert.That(a, Is.EqualTo(b).↓Within(0.1));");

            RoslynAssert.Diagnostics(analyzer, expectedDiagnostic, testCode);
        }

        [Test]
        public void AnalyzeWhenAppliedToEqualityConstraintForIncompatibleTuples()
        {
            string testCode = TestUtility.WrapInTestMethod(@"
                var a = (""1"", ""1"");
                var b = (""1.1"", ""1"");
                Assert.That(a, Is.EqualTo(b).↓Within(0.1));");

            RoslynAssert.Diagnostics(analyzer, expectedDiagnostic, testCode);
        }

        [Test]
        public void AnalyzeWhenAppliedToEqualityConstraintForMixedCompatibleTuples()
        {
            string testCode = TestUtility.WrapInTestMethod(@"
                var a = (1, ""1"");
                var b = (1.01, ""1"");
                Assert.That(a, Is.EqualTo(b).Within(0.1));");

            RoslynAssert.Valid(analyzer, testCode);
        }

        [Test]
        public void AnalyzeWhenAppliedToEqualityConstraintForNamedTuples()
        {
            string testCode = TestUtility.WrapInTestMethod(@"
                var a = (value: 1, name: ""1"");
                var b = (value: 1.01, name: ""1"");
                Assert.That(a, Is.EqualTo(b).Within(0.1));");

            RoslynAssert.Valid(analyzer, testCode);
        }

        [TestCase("1", "1")]
        [TestCase("0.1234", "0.2134")]
        public void AnalyzeWhenAppliedToEqualityConstraintForNumericTypes(string a, string b)
        {
            string testCode = TestUtility.WrapInTestMethod(
                $"Assert.That({a}, Is.EqualTo({b}).Within(0.1));");

            RoslynAssert.Valid(analyzer, testCode);
        }

        [Test]
        public void AnalyzeWhenAppliedToEqualityConstraintForNumericTuples()
        {
            string testCode = TestUtility.WrapInTestMethod(@"
                var a = (1, 1);
                var b = (1.1, 1);
                Assert.That(a, Is.EqualTo(b).Within(0.1));");

            RoslynAssert.Valid(analyzer, testCode);
        }

        [Test]
        public void AnalyzeWhenAppliedToEqualityConstraintForMixedValidTypeTuples()
        {
            string testCode = TestUtility.WrapInTestMethod(@"
                var a = (1, DateTime.Now);
                var b = (1.1, DateTime.Now);
                Assert.That(a, Is.EqualTo(b).Within(0.1));");

            RoslynAssert.Valid(analyzer, testCode);
        }

        [TestCase(nameof(DateTimeOffset))]
        [TestCase(nameof(DateTime))]
        public void AnalyzeWhenAppliedToEqualityConstraintForDateTypes(string typeName)
        {
            string testCode = TestUtility.WrapInTestMethod($@"
                var a = {typeName}.Now;
                var b = {typeName}.Now;
                Assert.That(a, Is.EqualTo(b).Within(TimeSpan.Zero));");

            RoslynAssert.Valid(analyzer, testCode);
        }

        [Test]
        public void AnalyzeWhenAppliedToEqualityConstraintForDateTypeKind()
        {
            string testCode = TestUtility.WrapInTestMethod($@"
                var a = DateTimeKind.Local;
                var b = DateTimeKind.Local;
                Assert.That(a, Is.EqualTo(b).↓Within(1));");

            RoslynAssert.Diagnostics(analyzer, expectedDiagnostic, testCode);
        }

        [Test]
        public void AnalyzeWhenAppliedToEqualityConstraintForTimeSpanTypes()
        {
            string testCode = TestUtility.WrapInTestMethod(@"
                var a = TimeSpan.Zero;
                var b = TimeSpan.Zero;
                Assert.That(a, Is.EqualTo(b).Within(TimeSpan.Zero));");

            RoslynAssert.Valid(analyzer, testCode);
        }

        [Test]
        public void AnalyzeWhenAppliedToCollectionsOfValidTypes()
        {
            string testCode = TestUtility.WrapInTestMethod(@"
                double value = 5.5;
                int value2 = 6;
                Assert.That(new List<double>() { value }, Is.EqualTo(new List<double> { value2 }).Within(2));",
                "using System.Collections.Generic;");

            RoslynAssert.Valid(analyzer, testCode);
        }

        [Test]
        public void AnalyzeWhenAppliedToCollectionsOfInValidTypes()
        {
            string testCode = TestUtility.WrapInTestMethod(@"
                string value = ""Value"";
                string value2 = ""Value2"";
                Assert.That(new List<string>() { value }, Is.EqualTo(new List<string> { value2 }).↓Within(2));",
                "using System.Collections.Generic;");

            RoslynAssert.Diagnostics(analyzer, expectedDiagnostic, testCode);
        }

        [Test]
        public void AnalyzeWhenAppliedToImmutableArrayOfValidTypes()
        {
            string testCode = TestUtility.WrapInTestMethod(@"
                double value = 5.5;
                int value2 = 6;
                Assert.That(ImmutableArray.Create(value), Is.EqualTo(ImmutableArray.Create(value2)).Within(2));",
                "using System.Collections.Immutable;");

            RoslynAssert.Valid(analyzer, testCode, this.settings);
        }

        [Test]
        public void AnalyzeWhenAppliedToImmutableArrayOfInValidTypes()
        {
            string testCode = TestUtility.WrapInTestMethod(@"
                string value = ""Value"";
                string value2 = ""Value2"";
                Assert.That(ImmutableArray.Create(value), Is.EqualTo(ImmutableArray.Create(value2)).↓Within(2));",
                "using System.Collections.Immutable;");

            RoslynAssert.Diagnostics(analyzer, expectedDiagnostic, testCode, this.settings);
        }

        [Test]
        public void AnalyzeWhenAppliedToChar()
        {
            string testCode = TestUtility.WrapInTestMethod($@"
                char a = 'a';
                char b = 'b';
                Assert.That(a, Is.EqualTo(b).↓Within(2));");

            RoslynAssert.Diagnostics(analyzer, expectedDiagnostic, testCode);
        }

        [Test]
        public void NestedCase()
        {
            string testCode = TestUtility.WrapInTestMethod($@"
                const string Key1 = ""key1"";
                const string Key2 = ""Key2"";
                const string Key3 = ""Key3"";
                var a = new KeyValuePair<string, KeyValuePair<string, KeyValuePair<string, int>>>(
                    Key1, new KeyValuePair<string, KeyValuePair<string, int>>(Key2, new KeyValuePair<string, int>(Key3, 3)));
                var b = new KeyValuePair<string, KeyValuePair<string, KeyValuePair<string, int>>>(
                    Key1, new KeyValuePair<string, KeyValuePair<string, int>>(Key2, new KeyValuePair<string, int>(Key3, 4)));
                Assert.That(a, Is.EqualTo(b).Within(2));",
            "using System.Collections.Generic;");

            RoslynAssert.Valid(analyzer, testCode);
        }

        [Test]
        public void AnalyzeNonGenericDictionaryEntry()
        {
            string testCode = TestUtility.WrapInTestMethod($@"
                const string Value = ""key"";
                var a = new DictionaryEntry(1, Value);
                var b = new DictionaryEntry(2, Value);
                Assert.That(a, Is.EqualTo(b).Within(2));",
                "using System.Collections;");

            // By the time we see the '.Within' we have no idea about the types used.
            RoslynAssert.Valid(analyzer, testCode);
        }

        [Test]
        public void AnalyzeOnNonGenericCollection()
        {
            string testCode = TestUtility.WrapInTestMethod(@"
                double value = 5.5;
                int value2 = 6;
                Assert.That(new ArrayList { value }, Is.EqualTo(new ArrayList { value2 }).Within(2));",
                "using System.Collections;");

            RoslynAssert.Valid(analyzer, testCode);
        }

        [Test]
        public void AnalyzeNonGenericSubclassOfGenericIEnumerable()
        {
            string testCode = TestUtility.WrapClassInNamespaceAndAddUsing(@"
                 [TestFixture]
                 public class TestClass
                 {
                     public void TestMethod()
                     {
                         Assert.That(new DoubleCollection(1), Is.EqualTo(new DoubleCollection(2)).Within(0.001));
                     }

                     private sealed class DoubleCollection : IEnumerable<double>
                     {
                         public DoubleCollection(params double[] numbers) { }
                         public IEnumerator<double> GetEnumerator() => throw new NotImplementedException();
                         IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                     }
                 }",
                additionalUsings: @"using System.Collections;
 using System.Collections.Generic;");

            RoslynAssert.Valid(analyzer, testCode);
        }

        [TestCase("double")]
        [TestCase("int")]
        [TestCase("TimeSpan")]
        public void AnalyzeWhenAppliedToEqualityConstraintForNullableValidTypes(string type)
        {
            string testCode = TestUtility.WrapMethodInClassNamespaceAndAddUsings($@"
                [TestCase(null, null)]
                public void Test({type}? a, {type}? b)
                {{
                    Assert.That(a, Is.EqualTo(b).Within(0.1));
                }}");

            RoslynAssert.Valid(analyzer, testCode);
        }

        [TestCase("class")]
        [TestCase("struct")]
        public void AnalyzeWhenAppliedToUserDefinedType(string kind)
        {
            string testCode = TestUtility.WrapClassInNamespaceAndAddUsing(@$"
    [TestFixture]
    public sealed class TestClass
    {{
        [Test]
        public void TestMethod()
        {{
            var instance = new UserDefinedType(1, 1.1, ""1.1"");

            Assert.That(new UserDefinedType(1, 1.2, ""1.1""), Is.EqualTo(instance).Within(0.1));
        }}

        private {kind} UserDefinedType
        {{
            public UserDefinedType(int valueA, double valueB, string valueC)
            {{
                ValueA = valueA;
                ValueB = valueB;
                ValueC = valueC;
            }}

            public int ValueA {{ get; }}
            public double ValueB {{ get; }}
            public string ValueC {{ get; }}
        }}
    }}");

            RoslynAssert.Valid(analyzer, testCode);
        }

        [TestCase("class")]
        [TestCase("struct")]
        public void AnalyzeWhenAppliedToUserDefinedTypeOverridingEquals(string kind)
        {
            string testCode = TestUtility.WrapClassInNamespaceAndAddUsing(@$"
    [TestFixture]
    public sealed class TestClass
    {{
        [Test]
        public void TestMethod()
        {{
            var instance = new UserDefinedType(1, 1.1, ""1.1"");

            Assert.That(new UserDefinedType(1, 1.2, ""1.1""), Is.EqualTo(instance).↓Within(0.1));
        }}

        private {kind} UserDefinedType
        {{
            public UserDefinedType(int valueA, double valueB, string valueC)
            {{
                ValueA = valueA;
                ValueB = valueB;
                ValueC = valueC;
            }}

            public int ValueA {{ get; }}
            public double ValueB {{ get; }}
            public string ValueC {{ get; }}

            public override bool Equals(object? obj)
            {{
                return obj is UserDefinedType other &&
                    other.ValueA == ValueA &&
                    other.ValueB == ValueB &&
                    other.ValueC == ValueC;
            }}

            public override int GetHashCode()
            {{
                return ValueA.GetHashCode() ^ ValueB.GetHashCode() ^ ValueC.GetHashCode();
            }}
        }}
    }}");

            RoslynAssert.Diagnostics(analyzer, expectedDiagnostic, testCode);
        }

        [Test]
        public void AnalyzeWhenAppliedToRecord()
        {
            string testCode = TestUtility.WrapClassInNamespaceAndAddUsing(@"
    [TestFixture]
    public sealed class TestClass
    {
        [Test]
        public void TestMethod()
        {
            var instance = new SomeRecord(1, 1.1, ""1.1"");

            Assert.That(new SomeRecord(1, 1.2, ""1.1""), Is.EqualTo(instance).↓Within(0.1));
        }

        private sealed record SomeRecord
        {
            public SomeRecord(int valueA, double valueB, string valueC)
            {
                ValueA = valueA;
                ValueB = valueB;
                ValueC = valueC;
            }

            public int ValueA { get; }
            public double ValueB { get; }
            public string ValueC { get; }
        }
    }");

            RoslynAssert.Diagnostics(analyzer, expectedDiagnostic, testCode);
        }
    }
}
