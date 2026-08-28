// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EricksonLopez.Specification.Analyzers;

/// <summary>
/// Provides a code fix for SPEC001: seals concrete specification classes.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SpecificationSealedCodeFixProvider)), Shared]
public sealed class SpecificationSealedCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [SpecificationDiagnosticDescriptors.SpecificationShouldBeSealedOrAbstract.Id];

    /// <inheritdoc/>
    public override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc/>
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        // Stryker disable once Boolean : Library best practice ConfigureAwait(false)
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        // Stryker disable once Statement : Defensive null guard
        if (root is null) return;

        // Stryker disable once Linq : Context has at least one diagnostic for registered fix
        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Stryker disable once Linq : Token ancestor lookup in valid syntax tree
        var declaration = root.FindToken(diagnosticSpan.Start).Parent!.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        // Stryker disable once Statement : Defensive null guard
        if (declaration is null) return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Make specification sealed",
                createChangedDocument: c => MakeSealedAsync(context.Document, declaration, c),
                equivalenceKey: nameof(SpecificationSealedCodeFixProvider)),
            diagnostic);
    }

    private static async Task<Document> MakeSealedAsync(Document document, ClassDeclarationSyntax classDeclaration, CancellationToken cancellationToken)
    {
        var sealedToken = SyntaxFactory.Token(SyntaxKind.SealedKeyword);
        var newModifiers = classDeclaration.Modifiers.Add(sealedToken);
        var newClassDeclaration = classDeclaration.WithModifiers(newModifiers);

        // Stryker disable once Boolean : Library best practice ConfigureAwait(false)
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null) return document;

        var newRoot = root.ReplaceNode(classDeclaration, newClassDeclaration);
        return document.WithSyntaxRoot(newRoot);
    }
}



