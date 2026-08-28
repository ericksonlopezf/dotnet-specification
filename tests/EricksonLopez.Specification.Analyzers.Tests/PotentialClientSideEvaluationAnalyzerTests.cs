// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace EricksonLopez.Specification.Analyzers.Tests;

public sealed class PotentialClientSideEvaluationAnalyzerTests
{
    [Fact]
    public void Descriptor_HasCorrectIdAndSeverity()
    {
        var descriptor = SpecificationDiagnosticDescriptors.PotentialClientSideEvaluation;
        descriptor.Id.Should().Be("SPEC007");
        descriptor.DefaultSeverity.Should().Be(DiagnosticSeverity.Warning);
    }

    [Fact]
    public async Task Analyzer_SafeBclMethods_NoDiagnostic()
    {
        var code = """
            using System.Linq.Expressions;

            public sealed class SafeSpec : Specification<string>
            {
                public override Expression<Func<string, bool>> BuildExpression()
                {
                    return x => x.Contains("test") && x.StartsWith("a") && x.Length > 2;
                }
            }
            """;

        await AnalyzerTestHelper.VerifyAnalyzerAsync<PotentialClientSideEvaluationAnalyzer>(code);
    }

    [Fact]
    public async Task Analyzer_CustomExternalMethod_ReportsDiagnostic()
    {
        var code = """
            using System.Linq.Expressions;

            public static class CustomHelper
            {
                public static bool IsValid(string s) => s.Length > 5;
            }

            public sealed class UnsafeSpec : Specification<string>
            {
                public override Expression<Func<string, bool>> BuildExpression()
                {
                    return x => {|#0:CustomHelper.IsValid(x)|};
                }
            }
            """;

        var expected = new DiagnosticResult("SPEC007", DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("IsValid");

        await AnalyzerTestHelper.VerifyAnalyzerAsync<PotentialClientSideEvaluationAnalyzer>(code, expected);
    }

    [Fact]
    public async Task Analyzer_EfFunctionsLike_NoDiagnostic()
    {
        var code = """
            using System.Linq.Expressions;

            public static class EF
            {
                public static readonly EfFunctions Functions = new();
            }

            public class EfFunctions
            {
                public bool Collate(string matchExpression, string collation) => true;
            }

            public sealed class EfSpec : Specification<string>
            {
                public override Expression<Func<string, bool>> BuildExpression()
                {
                    return x => EF.Functions.Collate(x, "nocase");
                }
            }
            """;

        await AnalyzerTestHelper.VerifyAnalyzerAsync<PotentialClientSideEvaluationAnalyzer>(code);
    }

    [Fact]
    public async Task Analyzer_NonSpecificationMethod_NoDiagnostic()
    {
        var code = """
            using System.Linq.Expressions;

            public static class CustomHelper
            {
                public static bool IsValid(string s) => s.Length > 5;
            }

            public class OtherService
            {
                public Expression<Func<string, bool>> BuildExpression()
                {
                    return x => CustomHelper.IsValid(x);
                }
            }
            """;

        await AnalyzerTestHelper.VerifyAnalyzerAsync<PotentialClientSideEvaluationAnalyzer>(code);
    }
}
