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

public sealed class IsSatisfiedByInExpressionAnalyzerTests
{
    [Fact]
    public void Descriptor_HasCorrectIdAndSeverity()
    {
        var descriptor = SpecificationDiagnosticDescriptors.IsSatisfiedByInsideBuildExpression;
        descriptor.Id.Should().Be("SPEC010");
        descriptor.DefaultSeverity.Should().Be(DiagnosticSeverity.Error);
        descriptor.IsEnabledByDefault.Should().BeTrue();
    }

    [Fact]
    public async Task Analyzer_ValidExpression_NoDiagnostic()
    {
        var code = """
            using EricksonLopez.Specification;
            
            public class ValidSpec : Specification<string>
            {
                public override System.Linq.Expressions.Expression<System.Func<string, bool>> BuildExpression() => x => x.StartsWith("test");
            }
            """;

        await AnalyzerTestHelper.VerifyAnalyzerAsync<IsSatisfiedByInExpressionAnalyzer>(code);
    }

    [Fact]
    public async Task Analyzer_IsSatisfiedByCalled_ReportsDiagnostic()
    {
        var code = """
            
            public class OtherSpec : Specification<string> 
            {
                public override System.Linq.Expressions.Expression<System.Func<string, bool>> BuildExpression() => x => true;
            }

            public class InvalidSpec : Specification<string>
            {
                private readonly OtherSpec _other = new OtherSpec();

                public override System.Linq.Expressions.Expression<System.Func<string, bool>> BuildExpression() => x => {|#0:_other.IsSatisfiedBy(x)|};
            }
            """;

        var expected = new DiagnosticResult("SPEC010", DiagnosticSeverity.Error)
            .WithLocation(0)
            .WithArguments("InvalidSpec");

        await AnalyzerTestHelper.VerifyAnalyzerAsync<IsSatisfiedByInExpressionAnalyzer>(code, expected);
    }

    [Fact]
    public async Task Analyzer_IsSatisfiedByOutsideBuildExpression_NoDiagnostic()
    {
        var code = """
            
            public class OtherSpec : Specification<string> 
            {
                public override System.Linq.Expressions.Expression<System.Func<string, bool>> BuildExpression() => x => true;
            }

            public class ValidSpec : Specification<string>
            {
                private readonly OtherSpec _other = new OtherSpec();

                public override System.Linq.Expressions.Expression<System.Func<string, bool>> BuildExpression() => x => true;

                public bool Check(string x)
                {
                    return _other.IsSatisfiedBy(x);
                }
            }
            """;

        await AnalyzerTestHelper.VerifyAnalyzerAsync<IsSatisfiedByInExpressionAnalyzer>(code);
    }

    [Fact]
    public async Task Analyzer_NormalClassWithBuildExpression_NoDiagnostic()
    {
        var code = """

            public class OtherSpec : Specification<string> 
            {
                public override System.Linq.Expressions.Expression<System.Func<string, bool>> BuildExpression() => x => true;
            }

            public class NormalService
            {
                private readonly OtherSpec _other = new OtherSpec();

                public void BuildExpression()
                {
                    _other.IsSatisfiedBy("test");
                }
            }
            """;

        await AnalyzerTestHelper.VerifyAnalyzerAsync<IsSatisfiedByInExpressionAnalyzer>(code);
    }

    [Fact]
    public async Task Analyzer_MultipleIsSatisfiedBy_ReportsFirstDiagnostic()
    {
        var code = """
            
            public class OtherSpec : Specification<string> 
            {
                public override System.Linq.Expressions.Expression<System.Func<string, bool>> BuildExpression() => x => true;
            }

            public class InvalidSpec : Specification<string>
            {
                private readonly OtherSpec _other = new OtherSpec();

                public override System.Linq.Expressions.Expression<System.Func<string, bool>> BuildExpression() => x => {|#0:_other.IsSatisfiedBy(x)|} || _other.IsSatisfiedBy(x);
            }
            """;

        var expected = new DiagnosticResult("SPEC010", DiagnosticSeverity.Error)
            .WithLocation(0)
            .WithArguments("InvalidSpec");

        await AnalyzerTestHelper.VerifyAnalyzerAsync<IsSatisfiedByInExpressionAnalyzer>(code, expected);
    }

    [Fact]
    public async Task Analyzer_IsSatisfiedByCalledDirectly_ReportsDiagnostic()
    {
        var code = """
            
            public class InvalidSpec : Specification<string>
            {
                public override System.Linq.Expressions.Expression<System.Func<string, bool>> BuildExpression() => x => {|#0:IsSatisfiedBy(x)|};
            }
            """;

        var expected = new DiagnosticResult("SPEC010", DiagnosticSeverity.Error)
            .WithLocation(0)
            .WithArguments("InvalidSpec");

        await AnalyzerTestHelper.VerifyAnalyzerAsync<IsSatisfiedByInExpressionAnalyzer>(code, expected);
    }

    [Fact]
    public async Task Analyzer_DelegateInvocation_NoDiagnostic()
    {
        var code = """
            
            public class ValidSpec : Specification<string>
            {
                public override System.Linq.Expressions.Expression<System.Func<string, bool>> BuildExpression() => x => ((System.Func<string, bool>)(y => true))(x);
            }
            """;

        await AnalyzerTestHelper.VerifyAnalyzerAsync<IsSatisfiedByInExpressionAnalyzer>(code);
    }
}





