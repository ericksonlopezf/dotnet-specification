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
/// Provides automated code fixes for SPEC011 migrating from Ardalis.Specification to EricksonLopez.Specification.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ArdalisMigrationCodeFixProvider)), Shared]
public sealed class ArdalisMigrationCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [SpecificationDiagnosticDescriptors.LegacyArdalisSpecificationDetected.Id];

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
                title: "Migrate using statement to EricksonLopez.Specification",
                createChangedDocument: c => MigrateNamespaceAsync(context.Document, c),
                equivalenceKey: nameof(ArdalisMigrationCodeFixProvider)),
            diagnostic);
    }

    private static async Task<Document> MigrateNamespaceAsync(Document document, CancellationToken cancellationToken)
    {
        // Stryker disable once Boolean : Library best practice ConfigureAwait(false)
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null) return document;

        var usings = root.DescendantNodes().OfType<UsingDirectiveSyntax>();
        UsingDirectiveSyntax? ardalisUsing = null;
        foreach (var u in usings)
        {
            if (u.Name?.ToString() == "Ardalis.Specification")
            {
                ardalisUsing = u;
                // Stryker disable once Statement : Early loop break optimization
                break;
            }
        }

        if (ardalisUsing is not null)
        {
            var newUsing = ardalisUsing.WithName(SyntaxFactory.ParseName("EricksonLopez.Specification"));
            var newRoot = root.ReplaceNode(ardalisUsing, newUsing);
            return document.WithSyntaxRoot(newRoot);
        }

        return document;
    }
}



