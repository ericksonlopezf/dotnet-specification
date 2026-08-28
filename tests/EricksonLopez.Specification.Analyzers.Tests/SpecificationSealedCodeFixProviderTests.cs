// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Xunit;

namespace EricksonLopez.Specification.Analyzers.Tests;

public sealed class SpecificationSealedCodeFixProviderTests
{
    [Fact]
    public async Task CodeFix_AddsSealedModifier()
    {
        var testCode = """
            using EricksonLopez.Specification;

            public class {|#0:MySpec|} : Specification<string>
            {
                public override System.Linq.Expressions.Expression<System.Func<string, bool>> BuildExpression() => x => true;
            }
            """;

        var fixedCode = """
            using EricksonLopez.Specification;

            public sealed class MySpec : Specification<string>
            {
                public override System.Linq.Expressions.Expression<System.Func<string, bool>> BuildExpression() => x => true;
            }
            """;

        var test = new CSharpCodeFixTest<SpecificationSealedAnalyzer, SpecificationSealedCodeFixProvider, DefaultVerifier>
        {
            TestCode = testCode,
            FixedCode = fixedCode,
            CompilerDiagnostics = CompilerDiagnostics.None
        };

        test.TestState.Sources.Add(AnalyzerTestHelper.SpecificationBaseMock);
        test.FixedState.Sources.Add(AnalyzerTestHelper.SpecificationBaseMock);

        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("SPEC001", DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments("MySpec"));

        await test.RunAsync();
    }

    [Fact]
    public void FixAllProvider_And_FixableDiagnosticIds_AreCorrect()
    {
        var provider = new SpecificationSealedCodeFixProvider();
        provider.FixableDiagnosticIds.Should().ContainSingle(id => id == "SPEC001");
        provider.GetFixAllProvider().Should().NotBeNull();
    }

    [Fact]
    public async Task RegisterCodeFixesAsync_RegistersExpectedCodeAction()
    {
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProj", LanguageNames.CSharp);
        var document = project.AddDocument("TestDoc.cs", "using EricksonLopez.Specification;\npublic class MySpec : Specification<string> {}");
        var syntaxTree = await document.GetSyntaxTreeAsync();
        var root = await syntaxTree!.GetRootAsync();
        var classDecl = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>().First();

        var diagnostic = Diagnostic.Create(
            SpecificationDiagnosticDescriptors.SpecificationShouldBeSealedOrAbstract,
            classDecl.Identifier.GetLocation(),
            "MySpec");

        var actions = new List<Microsoft.CodeAnalysis.CodeActions.CodeAction>();
        var context = new Microsoft.CodeAnalysis.CodeFixes.CodeFixContext(document, diagnostic, (a, d) => actions.Add(a), CancellationToken.None);

        var provider = new SpecificationSealedCodeFixProvider();
        await provider.RegisterCodeFixesAsync(context);

        actions.Should().ContainSingle();
        actions[0].Title.Should().Be("Make specification sealed");
        actions[0].EquivalenceKey.Should().Be(nameof(SpecificationSealedCodeFixProvider));
    }

    [Fact]
    public async Task RegisterCodeFixesAsync_WhenDeclarationIsNull_DoesNotRegisterCodeFix()
    {
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProj", LanguageNames.CSharp);
        var document = project.AddDocument("TestDoc.cs", "// empty file");

        var diagnostic = Diagnostic.Create(
            SpecificationDiagnosticDescriptors.SpecificationShouldBeSealedOrAbstract,
            Location.Create("TestDoc.cs", new Microsoft.CodeAnalysis.Text.TextSpan(0, 0), new Microsoft.CodeAnalysis.Text.LinePositionSpan(new Microsoft.CodeAnalysis.Text.LinePosition(0, 0), new Microsoft.CodeAnalysis.Text.LinePosition(0, 0))),
            "MySpec");

        var actions = new List<Microsoft.CodeAnalysis.CodeActions.CodeAction>();
        var context = new Microsoft.CodeAnalysis.CodeFixes.CodeFixContext(document, diagnostic, (a, d) => actions.Add(a), CancellationToken.None);

        var provider = new SpecificationSealedCodeFixProvider();
        await provider.RegisterCodeFixesAsync(context);

        actions.Should().BeEmpty();
    }
}






