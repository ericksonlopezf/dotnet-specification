// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace EricksonLopez.Specification.Analyzers.Tests;

public sealed class ExpressionInvokeAnalyzerTests
{
    [Fact]
    public void Descriptor_HasCorrectIdAndSeverity()
    {
        var descriptor = SpecificationDiagnosticDescriptors.ExpressionInvokeDetected;
        descriptor.Id.Should().Be("SPEC003");
        descriptor.DefaultSeverity.Should().Be(DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task Analyzer_BuildExpressionWithoutExpressionInvoke_NoDiagnostic()
    {
        var code = """
            using System.Linq.Expressions;

            public sealed class SafeSpec : Specification<string>
            {
                public override Expression<Func<string, bool>> BuildExpression()
                {
                    return x => x.Length > 0;
                }
            }
            """;

        await AnalyzerTestHelper.VerifyAnalyzerAsync<ExpressionInvokeAnalyzer>(code);
    }

    [Fact]
    public async Task Analyzer_BuildExpressionWithExpressionInvoke_ReportsDiagnostic()
    {
        var code = """
            using System.Linq.Expressions;

            public sealed class UnsafeSpec : Specification<string>
            {
                public override Expression<Func<string, bool>> BuildExpression()
                {
                    Expression<Func<string, bool>> expr = s => s.Length > 0;
                    var param = Expression.Parameter(typeof(string), "x");
                    var invoked = {|#0:Expression.Invoke(expr, param)|};
                    return Expression.Lambda<Func<string, bool>>(invoked, param);
                }
            }
            """;

        var expected = new DiagnosticResult("SPEC003", DiagnosticSeverity.Error)
            .WithLocation(0)
            .WithArguments("UnsafeSpec");

        await AnalyzerTestHelper.VerifyAnalyzerAsync<ExpressionInvokeAnalyzer>(code, expected);
    }

    [Fact]
    public async Task Analyzer_NonSpecificationMethodWithExpressionInvoke_NoDiagnostic()
    {
        var code = """
            using System.Linq.Expressions;

            public class OtherService
            {
                public void BuildExpression()
                {
                    Expression<Func<string, bool>> expr = s => s.Length > 0;
                    var param = Expression.Parameter(typeof(string), "x");
                    var invoked = Expression.Invoke(expr, param);
                }
            }
            """;

        await AnalyzerTestHelper.VerifyAnalyzerAsync<ExpressionInvokeAnalyzer>(code);
    }
}
