// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace EricksonLopez.Specification.Analyzers.Tests;

public sealed class UnboundedQueryAnalyzerTests
{
    [Fact]
    public void Descriptor_HasCorrectIdAndSeverity()
    {
        var descriptor = SpecificationDiagnosticDescriptors.UnboundedQuery;
        descriptor.Id.Should().Be("SPEC004");
        descriptor.DefaultSeverity.Should().Be(DiagnosticSeverity.Info);
    }

    [Fact]
    public async Task Analyzer_BoundedQueryWithTake_NoDiagnostic()
    {
        var code = """
            using EricksonLopez.Specification;

            public class Service
            {
                public void Query()
                {
                    var spec = QuerySpec<string>.Empty
                        .Where(x => x.Length > 0)
                        .Take(50);
                }
            }
            """;

        await AnalyzerTestHelper.VerifyAnalyzerAsync<UnboundedQueryAnalyzer>(code);
    }

    [Fact]
    public async Task Analyzer_BoundedQueryWithPage_NoDiagnostic()
    {
        var code = """
            using EricksonLopez.Specification;

            public class Service
            {
                public void Query()
                {
                    var spec = QuerySpec<string>.Empty
                        .Where(x => x.Length > 0)
                        .Page(1, 20);
                }
            }
            """;

        await AnalyzerTestHelper.VerifyAnalyzerAsync<UnboundedQueryAnalyzer>(code);
    }

    [Fact]
    public async Task Analyzer_UnboundedQuery_ReportsDiagnostic()
    {
        var code = """
            using EricksonLopez.Specification;

            public class Service
            {
                public void Query()
                {
                    var {|#0:spec|} = QuerySpec<string>.Empty
                        .Where(x => x.Length > 0);
                }
            }
            """;

        var expected = new DiagnosticResult("SPEC004", DiagnosticSeverity.Info)
            .WithLocation(0)
            .WithArguments("String");

        await AnalyzerTestHelper.VerifyAnalyzerAsync<UnboundedQueryAnalyzer>(code, expected);
    }

    [Fact]
    public async Task Analyzer_UninitializedVariable_NoDiagnostic()
    {
        var code = """
            using EricksonLopez.Specification;

            public class Service
            {
                public void Query()
                {
                    QuerySpec<string> spec;
                }
            }
            """;

        await AnalyzerTestHelper.VerifyAnalyzerAsync<UnboundedQueryAnalyzer>(code);
    }

    [Fact]
    public async Task Analyzer_NonQuerySpecVariable_NoDiagnostic()
    {
        var code = """
            public class Service
            {
                public void Query()
                {
                    var s = "hello";
                }
            }
            """;

        await AnalyzerTestHelper.VerifyAnalyzerAsync<UnboundedQueryAnalyzer>(code);
    }

    [Fact]
    public void IsQuerySpecType_WhenTypeNull_ReturnsFalse()
    {
        var method = typeof(UnboundedQueryAnalyzer).GetMethod("IsQuerySpecType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var result = (bool)method.Invoke(null, new object?[] { null })!;
        result.Should().BeFalse();
    }

    [Fact]
    public void GetEntityTypeName_WhenTypeNonGenericOrNull_ReturnsFallback()
    {
        var method = typeof(UnboundedQueryAnalyzer).GetMethod("GetEntityTypeName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var result = (string)method.Invoke(null, new object?[] { null })!;
        result.Should().Be("T");
    }
}
