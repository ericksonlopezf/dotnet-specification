// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Xunit;

namespace EricksonLopez.Specification.Analyzers.Tests;

public sealed class ArdalisMigrationAnalyzerTests
{
    private const string ArdalisMock = """
        namespace Ardalis.Specification
        {
            public abstract class Specification<T> { }
        }
        """;

    [Fact]
    public void Descriptor_HasCorrectIdAndSeverity()
    {
        var descriptor = SpecificationDiagnosticDescriptors.LegacyArdalisSpecificationDetected;
        descriptor.Id.Should().Be("SPEC011");
        descriptor.DefaultSeverity.Should().Be(DiagnosticSeverity.Info);
        descriptor.IsEnabledByDefault.Should().BeTrue();
    }

    [Fact]
    public async Task Analyzer_ArdalisSpecification_ReportsDiagnostic()
    {
        var code = """
            using Ardalis.Specification;

            public class {|#0:LegacySpec|} : Specification<string>
            {
            }
            """;

        var test = new CSharpAnalyzerTest<ArdalisMigrationAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            CompilerDiagnostics = CompilerDiagnostics.None
        };

        test.TestState.Sources.Add(ArdalisMock);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("SPEC011", DiagnosticSeverity.Info)
                .WithLocation(0)
                .WithArguments("LegacySpec"));

        await test.RunAsync();
    }

    [Fact]
    public async Task Analyzer_EricksonLopezSpecification_NoDiagnostic()
    {
        var code = """
            using EricksonLopez.Specification;

            public sealed class ModernSpec : Specification<string>
            {
                public override System.Linq.Expressions.Expression<System.Func<string, bool>> BuildExpression() => x => true;
            }
            """;

        var test = new CSharpAnalyzerTest<ArdalisMigrationAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            CompilerDiagnostics = CompilerDiagnostics.None
        };

        test.TestState.Sources.Add(AnalyzerTestHelper.SpecificationBaseMock);

        await test.RunAsync();
    }

    [Fact]
    public async Task Analyzer_CustomSpecificationInOtherNamespace_NoDiagnostic()
    {
        var code = """
            namespace MyCustom
            {
                public abstract class Specification<T> { }
            }

            public class CustomSpec : MyCustom.Specification<string>
            {
            }
            """;

        var test = new CSharpAnalyzerTest<ArdalisMigrationAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            CompilerDiagnostics = CompilerDiagnostics.None
        };

        test.TestState.Sources.Add(ArdalisMock);

        await test.RunAsync();
    }

    [Fact]
    public async Task Analyzer_SpecificationInGlobalNamespace_NoDiagnostic()
    {
        var code = """
            public abstract class Specification { }

            public class GlobalSpec : Specification
            {
            }
            """;

        var test = new CSharpAnalyzerTest<ArdalisMigrationAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            CompilerDiagnostics = CompilerDiagnostics.None
        };

        await test.RunAsync();
    }
}






