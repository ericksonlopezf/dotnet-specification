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

public sealed class AsyncInExpressionAnalyzerTests
{
    [Fact]
    public void Descriptor_HasCorrectIdAndSeverity()
    {
        var descriptor = SpecificationDiagnosticDescriptors.AsyncLambdaInExpression;
        descriptor.Id.Should().Be("SPEC009");
        descriptor.DefaultSeverity.Should().Be(DiagnosticSeverity.Error);
        descriptor.IsEnabledByDefault.Should().BeTrue();
    }

    [Fact]
    public async Task Analyzer_ValidSynchronousExpression_NoDiagnostic()
    {
        var code = """
            using EricksonLopez.Specification;
            
            public class ValidSpec : Specification<string>
            {
                public override System.Linq.Expressions.Expression<System.Func<string, bool>> BuildExpression() => x => x.Length > 0;
            }
            """;

        await AnalyzerTestHelper.VerifyAnalyzerAsync<AsyncInExpressionAnalyzer>(code);
    }

    [Fact]
    public async Task Analyzer_AsyncLambda_ReportsDiagnostic()
    {
        var code = """
            
            public class InvalidSpec : Specification<string>
            {
                public override System.Linq.Expressions.Expression<System.Func<string, bool>> BuildExpression() => {|#0:async|} x => x.Length > 0;
            }
            """;

        var expected = new DiagnosticResult("SPEC009", DiagnosticSeverity.Error)
            .WithLocation(0)
            .WithArguments("InvalidSpec");

        await AnalyzerTestHelper.VerifyAnalyzerAsync<AsyncInExpressionAnalyzer>(code, expected);
    }

    [Fact]
    public async Task Analyzer_AwaitExpression_ReportsDiagnostic()
    {
        var code = """
            
            public class InvalidSpec : Specification<string>
            {
                public override System.Linq.Expressions.Expression<System.Func<string, bool>> BuildExpression() 
                {
                    var task = Task.FromResult(true);
                    // This is invalid code anyway (won't compile because it's not async method),
                    // but we test that our analyzer catches await keyword
                    return x => {|#0:await|} task;
                }
            }
            """;

        var expected = new DiagnosticResult("SPEC009", DiagnosticSeverity.Error)
            .WithLocation(0)
            .WithArguments("InvalidSpec");

        await AnalyzerTestHelper.VerifyAnalyzerAsync<AsyncInExpressionAnalyzer>(code, expected);
    }

    [Fact]
    public async Task Analyzer_OutsideBuildExpression_NoDiagnostic()
    {
        var code = """
            
            public class ValidSpec : Specification<string>
            {
                public override System.Linq.Expressions.Expression<System.Func<string, bool>> BuildExpression() => x => true;
                
                public async Task DoSomethingAsync()
                {
                    await Task.Yield();
                }
            }
            """;

        await AnalyzerTestHelper.VerifyAnalyzerAsync<AsyncInExpressionAnalyzer>(code);
    }

    [Fact]
    public async Task Analyzer_NormalClassWithBuildExpression_NoDiagnostic()
    {
        var code = """
            
            public class NormalService
            {
                public async Task BuildExpression()
                {
                    await Task.Yield();
                }
            }
            """;

        await AnalyzerTestHelper.VerifyAnalyzerAsync<AsyncInExpressionAnalyzer>(code);
    }

    [Fact]
    public async Task Analyzer_MultipleAsyncLambdas_ReportsFirstDiagnostic()
    {
        var code = """
            
            public class InvalidSpec : Specification<string>
            {
                public override System.Linq.Expressions.Expression<System.Func<string, bool>> BuildExpression()
                {
                    System.Func<Task> func1 = {|#0:async|} () => { };
                    System.Func<Task> func2 = async () => { };
                    return x => true;
                }
            }
            """;

        var expected = new DiagnosticResult("SPEC009", DiagnosticSeverity.Error)
            .WithLocation(0)
            .WithArguments("InvalidSpec");

        await AnalyzerTestHelper.VerifyAnalyzerAsync<AsyncInExpressionAnalyzer>(code, expected);
    }
}




