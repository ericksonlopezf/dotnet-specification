// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace EricksonLopez.Specification.Analyzers.Tests;

public static class AnalyzerTestHelper
{
    public const string SpecificationBaseMock = """
        namespace EricksonLopez.Specification
        {
            public abstract class Specification<T>
            {
                public abstract System.Linq.Expressions.Expression<System.Func<T, bool>> BuildExpression();
                public bool IsSatisfiedBy(T entity) => throw new System.NotImplementedException();
            }
        }
        """;

    public const string UsingsMock = """
        global using System;
        global using System.Collections.Generic;
        global using System.Linq;
        global using System.Linq.Expressions;
        global using System.Threading;
        global using System.Threading.Tasks;
        global using EricksonLopez.Specification;
        """;

    public static async Task VerifyAnalyzerAsync<TAnalyzer>(string source, params DiagnosticResult[] expected)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        var test = new CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            CompilerDiagnostics = CompilerDiagnostics.None
        };

        // Add the mock Specification<T> and usings to the compilation
        test.TestState.Sources.Add(SpecificationBaseMock);
        test.TestState.Sources.Add(UsingsMock);

        test.ExpectedDiagnostics.AddRange(expected);

        await test.RunAsync();
    }
}




