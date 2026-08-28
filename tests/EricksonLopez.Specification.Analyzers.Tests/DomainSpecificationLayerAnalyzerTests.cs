// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace EricksonLopez.Specification.Analyzers.Tests;

public sealed class DomainSpecificationLayerAnalyzerTests
{
    [Fact]
    public void Descriptor_HasCorrectIdAndSeverity()
    {
        var descriptor = SpecificationDiagnosticDescriptors.DomainSpecificationOutsideDomain;
        descriptor.Id.Should().Be("SPEC006");
        descriptor.DefaultSeverity.Should().Be(DiagnosticSeverity.Info);
    }

    [Fact]
    public async Task Analyzer_SpecificationInDomainNamespace_NoDiagnostic()
    {
        var code = """
            namespace MyApp.Domain.Specifications
            {
                public sealed class CustomerSpec : Specification<string>
                {
                    public override System.Linq.Expressions.Expression<System.Func<string, bool>> BuildExpression() => x => true;
                }
            }
            """;

        await AnalyzerTestHelper.VerifyAnalyzerAsync<DomainSpecificationLayerAnalyzer>(code);
    }

    [Fact]
    public async Task Analyzer_SpecificationInInfrastructureNamespace_ReportsDiagnostic()
    {
        var code = """
            namespace MyApp.Infrastructure.Data
            {
                public sealed class {|#0:CustomerSpec|} : Specification<string>
                {
                    public override System.Linq.Expressions.Expression<System.Func<string, bool>> BuildExpression() => x => true;
                }
            }
            """;

        var expected = new DiagnosticResult("SPEC006", DiagnosticSeverity.Info)
            .WithLocation(0)
            .WithArguments("CustomerSpec", "MyApp.Infrastructure.Data");

        await AnalyzerTestHelper.VerifyAnalyzerAsync<DomainSpecificationLayerAnalyzer>(code, expected);
    }

    [Fact]
    public async Task Analyzer_NonSpecificationInInfrastructureNamespace_NoDiagnostic()
    {
        var code = """
            namespace MyApp.Infrastructure.Data
            {
                public class CustomerRepository
                {
                }
            }
            """;

        await AnalyzerTestHelper.VerifyAnalyzerAsync<DomainSpecificationLayerAnalyzer>(code);
    }
}
