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

public sealed class ArdalisMigrationCodeFixProviderTests
{
    private const string ArdalisMock = """
        namespace Ardalis.Specification
        {
            public abstract class Specification<T> { }
        }
        """;

    [Fact]
    public async Task CodeFix_MigratesNamespaceToEricksonLopez()
    {
        var testCode = """
            using Ardalis.Specification;

            public class {|#0:LegacySpec|} : Specification<string>
            {
            }
            """;

        var fixedCode = """
            using EricksonLopez.Specification;

            public class LegacySpec : Specification<string>
            {
            }
            """;

        var test = new CSharpCodeFixTest<ArdalisMigrationAnalyzer, ArdalisMigrationCodeFixProvider, DefaultVerifier>
        {
            TestCode = testCode,
            FixedCode = fixedCode,
            CompilerDiagnostics = CompilerDiagnostics.None
        };

        test.TestState.Sources.Add(ArdalisMock);
        test.TestState.Sources.Add(AnalyzerTestHelper.SpecificationBaseMock);
        test.FixedState.Sources.Add(ArdalisMock);
        test.FixedState.Sources.Add(AnalyzerTestHelper.SpecificationBaseMock);

        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("SPEC011", DiagnosticSeverity.Info)
                .WithLocation(0)
                .WithArguments("LegacySpec"));

        await test.RunAsync();
    }

    [Fact]
    public void FixAllProvider_And_FixableDiagnosticIds_AreCorrect()
    {
        var provider = new ArdalisMigrationCodeFixProvider();
        provider.FixableDiagnosticIds.Should().ContainSingle(id => id == "SPEC011");
        provider.GetFixAllProvider().Should().NotBeNull();
    }

    [Fact]
    public async Task MigrateNamespaceAsync_WhenNoArdalisUsing_ReturnsOriginalDocument()
    {
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProj", LanguageNames.CSharp);
        var document = project.AddDocument("TestDoc.cs", "using System; public class Foo {}");

        var method = typeof(ArdalisMigrationCodeFixProvider).GetMethod(
            "MigrateNamespaceAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;

        var task = (Task<Document>)method.Invoke(null, new object[] { document, CancellationToken.None })!;
        var resultDoc = await task;

        var text = (await resultDoc.GetTextAsync()).ToString();
        text.Should().Be("using System; public class Foo {}");
    }

    [Fact]
    public async Task RegisterCodeFixesAsync_RegistersExpectedCodeAction()
    {
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProj", LanguageNames.CSharp);
        var document = project.AddDocument("TestDoc.cs", "using Ardalis.Specification;\npublic class LegacySpec : Specification<string> {}");
        var syntaxTree = await document.GetSyntaxTreeAsync();
        var root = await syntaxTree!.GetRootAsync();
        var classDecl = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>().First();

        var diagnostic = Diagnostic.Create(
            SpecificationDiagnosticDescriptors.LegacyArdalisSpecificationDetected,
            classDecl.Identifier.GetLocation(),
            "LegacySpec");

        var actions = new List<Microsoft.CodeAnalysis.CodeActions.CodeAction>();
        var context = new Microsoft.CodeAnalysis.CodeFixes.CodeFixContext(document, diagnostic, (a, d) => actions.Add(a), CancellationToken.None);

        var provider = new ArdalisMigrationCodeFixProvider();
        await provider.RegisterCodeFixesAsync(context);

        actions.Should().ContainSingle();
        actions[0].Title.Should().Be("Migrate using statement to EricksonLopez.Specification");
        actions[0].EquivalenceKey.Should().Be(nameof(ArdalisMigrationCodeFixProvider));
    }

    [Fact]
    public async Task RegisterCodeFixesAsync_WhenDeclarationIsNull_DoesNotRegisterCodeFix()
    {
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProj", LanguageNames.CSharp);
        var document = project.AddDocument("TestDoc.cs", "// empty file");

        var diagnostic = Diagnostic.Create(
            SpecificationDiagnosticDescriptors.LegacyArdalisSpecificationDetected,
            Location.Create("TestDoc.cs", new Microsoft.CodeAnalysis.Text.TextSpan(0, 0), new Microsoft.CodeAnalysis.Text.LinePositionSpan(new Microsoft.CodeAnalysis.Text.LinePosition(0, 0), new Microsoft.CodeAnalysis.Text.LinePosition(0, 0))),
            "LegacySpec");

        var actions = new List<Microsoft.CodeAnalysis.CodeActions.CodeAction>();
        var context = new Microsoft.CodeAnalysis.CodeFixes.CodeFixContext(document, diagnostic, (a, d) => actions.Add(a), CancellationToken.None);

        var provider = new ArdalisMigrationCodeFixProvider();
        await provider.RegisterCodeFixesAsync(context);

        actions.Should().BeEmpty();
    }
}






