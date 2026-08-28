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

public sealed class SpecificationMutableStateAnalyzerTests
{
    [Fact]
    public void Descriptor_HasCorrectIdAndSeverity()
    {
        var descriptor = SpecificationDiagnosticDescriptors.MutableStateInSpecification;
        descriptor.Id.Should().Be("SPEC002");
        descriptor.DefaultSeverity.Should().Be(DiagnosticSeverity.Warning);
        descriptor.IsEnabledByDefault.Should().BeTrue();
    }

    [Fact]
    public async Task Analyzer_ReadOnlyField_NoDiagnostic()
    {
        var code = """
            using EricksonLopez.Specification;
            
            public sealed class ValidSpec : Specification<string>
            {
                private readonly int _age;
                public ValidSpec(int age) => _age = age;
                public override System.Linq.Expressions.Expression<System.Func<string, bool>> BuildExpression() => x => true;
            }
            """;

        await AnalyzerTestHelper.VerifyAnalyzerAsync<SpecificationMutableStateAnalyzer>(code);
    }

    [Fact]
    public async Task Analyzer_ConstField_NoDiagnostic()
    {
        var code = """
            
            public sealed class ValidSpec : Specification<string>
            {
                private const int Age = 10;
                public override System.Linq.Expressions.Expression<System.Func<string, bool>> BuildExpression() => x => true;
            }
            """;

        await AnalyzerTestHelper.VerifyAnalyzerAsync<SpecificationMutableStateAnalyzer>(code);
    }

    [Fact]
    public async Task Analyzer_StaticField_NoDiagnostic()
    {
        var code = """
            
            public sealed class ValidSpec : Specification<string>
            {
                private static int _age;
                public override System.Linq.Expressions.Expression<System.Func<string, bool>> BuildExpression() => x => true;
            }
            """;

        await AnalyzerTestHelper.VerifyAnalyzerAsync<SpecificationMutableStateAnalyzer>(code);
    }

    [Fact]
    public async Task Analyzer_PropertyWithPrivateSetter_NoDiagnostic()
    {
        var code = """
            
            public sealed class ValidSpec : Specification<string>
            {
                public int Age { get; private set; }
                public override System.Linq.Expressions.Expression<System.Func<string, bool>> BuildExpression() => x => true;
            }
            """;

        await AnalyzerTestHelper.VerifyAnalyzerAsync<SpecificationMutableStateAnalyzer>(code);
    }

    [Fact]
    public async Task Analyzer_MutableField_ReportsDiagnostic()
    {
        var code = """
            
            public sealed class InvalidSpec : Specification<string>
            {
                private int {|#0:_age|};
                public override System.Linq.Expressions.Expression<System.Func<string, bool>> BuildExpression() => x => true;
            }
            """;

        var expected = new DiagnosticResult("SPEC002", DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("_age", "InvalidSpec");

        await AnalyzerTestHelper.VerifyAnalyzerAsync<SpecificationMutableStateAnalyzer>(code, expected);
    }

    [Fact]
    public async Task Analyzer_MutableProperty_ReportsDiagnostic()
    {
        var code = """
            
            public sealed class InvalidSpec : Specification<string>
            {
                public int {|#0:Age|} { get; set; }
                public override System.Linq.Expressions.Expression<System.Func<string, bool>> BuildExpression() => x => true;
            }
            """;

        var expected = new DiagnosticResult("SPEC002", DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("Age", "InvalidSpec");

        await AnalyzerTestHelper.VerifyAnalyzerAsync<SpecificationMutableStateAnalyzer>(code, expected);
    }

    [Fact]
    public async Task Analyzer_NotInheritingSpecification_NoDiagnostic()
    {
        var code = """
            public class MutableService
            {
                private int _age;
                public int Age { get; set; }
            }
            """;

        await AnalyzerTestHelper.VerifyAnalyzerAsync<SpecificationMutableStateAnalyzer>(code);
    }

    [Fact]
    public async Task Analyzer_ReadOnlyProperty_NoDiagnostic()
    {
        var code = """
            
            public sealed class ValidSpec : Specification<string>
            {
                public int Age { get; }
                public ValidSpec(int age) => Age = age;
                public override System.Linq.Expressions.Expression<System.Func<string, bool>> BuildExpression() => x => true;
            }
            """;

        await AnalyzerTestHelper.VerifyAnalyzerAsync<SpecificationMutableStateAnalyzer>(code);
    }

    [Fact]
    public async Task Analyzer_StaticProperty_NoDiagnostic()
    {
        var code = """
            
            public sealed class ValidSpec : Specification<string>
            {
                public static int Age { get; set; }
                public override System.Linq.Expressions.Expression<System.Func<string, bool>> BuildExpression() => x => true;
            }
            """;

        await AnalyzerTestHelper.VerifyAnalyzerAsync<SpecificationMutableStateAnalyzer>(code);
    }

    [Fact]
    public async Task Analyzer_AbstractProperty_NoDiagnostic()
    {
        var code = """
            
            public abstract class BaseSpec : Specification<string>
            {
                public abstract int Age { get; set; }
                public override System.Linq.Expressions.Expression<System.Func<string, bool>> BuildExpression() => x => true;
            }
            """;

        await AnalyzerTestHelper.VerifyAnalyzerAsync<SpecificationMutableStateAnalyzer>(code);
    }
}





