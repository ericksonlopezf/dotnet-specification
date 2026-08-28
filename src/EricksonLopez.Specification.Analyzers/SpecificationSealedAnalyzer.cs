// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EricksonLopez.Specification.Analyzers;

/// <summary>
/// Detects when a concrete class inheriting from Specification&lt;T&gt; is neither sealed nor abstract.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SpecificationSealedAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [SpecificationDiagnosticDescriptors.SpecificationShouldBeSealedOrAbstract];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        // Stryker disable once all : Roslyn analyzer lifecycle configuration
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        // Stryker disable once all : Roslyn analyzer lifecycle configuration
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        var type = (INamedTypeSymbol)context.Symbol;


        // Skip abstract and sealed types — they're fine
        if (type.IsAbstract || type.IsSealed)
            return;

        // Check if the type inherits from Specification<T>
        if (!type.InheritsFromSpecification())
            return;

        // Report the diagnostic
        var declaration = (ClassDeclarationSyntax)type.DeclaringSyntaxReferences[0].GetSyntax();

        var diagnostic = Diagnostic.Create(
            SpecificationDiagnosticDescriptors.SpecificationShouldBeSealedOrAbstract,
            declaration.Identifier.GetLocation(),
            type.Name);

        context.ReportDiagnostic(diagnostic);
    }


}


