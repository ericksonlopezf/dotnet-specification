// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace EricksonLopez.Specification.Analyzers.Tests;

public sealed class SpecificationSealedAnalyzerTests
{
    [Fact]
    public void Descriptor_HasCorrectIdAndSeverity()
    {
        var descriptor = SpecificationDiagnosticDescriptors.SpecificationShouldBeSealedOrAbstract;
        descriptor.Id.Should().Be("SPEC001");
        descriptor.DefaultSeverity.Should().Be(DiagnosticSeverity.Warning);
        descriptor.IsEnabledByDefault.Should().BeTrue();
    }

    [Fact]
    public async Task Analyzer_SealedSpecification_NoDiagnostic()
    {
        var code = """
            using EricksonLopez.Specification;
            
            public sealed class ValidSpec : Specification<string>
            {
                public override System.Linq.Expressions.Expression<System.Func<string, bool>> BuildExpression() => x => true;
            }
            """;

        await AnalyzerTestHelper.VerifyAnalyzerAsync<SpecificationSealedAnalyzer>(code);
    }

    [Fact]
    public async Task Analyzer_AbstractSpecification_NoDiagnostic()
    {
        var code = """
            
            public abstract class BaseSpec : Specification<string>
            {
            }
            """;

        await AnalyzerTestHelper.VerifyAnalyzerAsync<SpecificationSealedAnalyzer>(code);
    }

    [Fact]
    public async Task Analyzer_UnsealedSpecification_ReportsDiagnostic()
    {
        var code = """
            
            public class {|#0:InvalidSpec|} : Specification<string>
            {
                public override System.Linq.Expressions.Expression<System.Func<string, bool>> BuildExpression() => x => true;
            }
            """;

        var expected = new DiagnosticResult("SPEC001", DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("InvalidSpec");

        await AnalyzerTestHelper.VerifyAnalyzerAsync<SpecificationSealedAnalyzer>(code, expected);
    }

    [Fact]
    public async Task Analyzer_NotInheritingSpecification_NoDiagnostic()
    {
        var code = """
            public class UnsealedService
            {
            }
            """;

        await AnalyzerTestHelper.VerifyAnalyzerAsync<SpecificationSealedAnalyzer>(code);
    }

    [Fact]
    public async Task Analyzer_DifferentNamespaceSpecification_NoDiagnostic()
    {
        var code = """
            namespace Fake.Specification
            {
                public class Specification<T> { }
            }

            public class UnsealedSpec : Fake.Specification.Specification<string>
            {
            }
            """;

        await AnalyzerTestHelper.VerifyAnalyzerAsync<SpecificationSealedAnalyzer>(code);
    }

    [Fact]
    public async Task Analyzer_DifferentTypeArgumentsSpecification_NoDiagnostic()
    {
        var code = """
            namespace EricksonLopez.Specification
            {
                public class Specification<T1, T2> { }
            }

            public class UnsealedSpec : EricksonLopez.Specification.Specification<string, int>
            {
            }
            """;

        await AnalyzerTestHelper.VerifyAnalyzerAsync<SpecificationSealedAnalyzer>(code);
    }

    [Fact]
    public async Task Analyzer_Interface_NoDiagnostic()
    {
        var code = """
            public interface IMyInterface
            {
            }
            """;

        await AnalyzerTestHelper.VerifyAnalyzerAsync<SpecificationSealedAnalyzer>(code);
    }

    [Fact]
    public async Task Analyzer_GlobalNamespaceSpecification_NoDiagnostic()
    {
        var code = """
            public class Specification<T> { }

            public class UnsealedSpec : Specification<string>
            {
            }
            """;

        await AnalyzerTestHelper.VerifyAnalyzerAsync<SpecificationSealedAnalyzer>(code);
    }
}





