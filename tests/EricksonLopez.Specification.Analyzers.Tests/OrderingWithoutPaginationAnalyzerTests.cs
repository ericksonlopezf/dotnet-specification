// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace EricksonLopez.Specification.Analyzers.Tests;

public sealed class OrderingWithoutPaginationAnalyzerTests
{
    [Fact]
    public void Descriptor_HasCorrectIdAndSeverity()
    {
        var descriptor = SpecificationDiagnosticDescriptors.OrderingWithoutPagination;
        descriptor.Id.Should().Be("SPEC005");
        descriptor.DefaultSeverity.Should().Be(DiagnosticSeverity.Info);
    }

    [Fact]
    public async Task Analyzer_OrderedAndPaginated_NoDiagnostic()
    {
        var code = """
            using EricksonLopez.Specification;

            public class Service
            {
                public void Query()
                {
                    var spec = QuerySpec<string>.Empty
                        .OrderBy(x => x.Length)
                        .Take(10);
                }
            }
            """;

        await AnalyzerTestHelper.VerifyAnalyzerAsync<OrderingWithoutPaginationAnalyzer>(code);
    }

    [Fact]
    public async Task Analyzer_OrderedWithoutPagination_ReportsDiagnostic()
    {
        var code = """
            using EricksonLopez.Specification;

            public class Service
            {
                public void Query()
                {
                    var {|#0:spec|} = QuerySpec<string>.Empty
                        .OrderBy(x => x.Length);
                }
            }
            """;

        var expected = new DiagnosticResult("SPEC005", DiagnosticSeverity.Info)
            .WithLocation(0)
            .WithArguments("String");

        await AnalyzerTestHelper.VerifyAnalyzerAsync<OrderingWithoutPaginationAnalyzer>(code, expected);
    }

    [Fact]
    public async Task Analyzer_NoOrdering_NoDiagnostic()
    {
        var code = """
            using EricksonLopez.Specification;

            public class Service
            {
                public void Query()
                {
                    var spec = QuerySpec<string>.Empty
                        .Where(x => x.Length > 0);
                }
            }
            """;

        await AnalyzerTestHelper.VerifyAnalyzerAsync<OrderingWithoutPaginationAnalyzer>(code);
    }
}
