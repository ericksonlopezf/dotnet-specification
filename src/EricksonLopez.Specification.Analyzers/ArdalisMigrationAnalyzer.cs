// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EricksonLopez.Specification.Analyzers;

/// <summary>
/// Detects classes inheriting from Ardalis.Specification to facilitate automated migration to EricksonLopez.Specification.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ArdalisMigrationAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [SpecificationDiagnosticDescriptors.LegacyArdalisSpecificationDetected];

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

        var baseType = type.BaseType;
        while (baseType is not null)
        {
            if (baseType.Name == "Specification" &&
                baseType.ContainingNamespace.ToString() == "Ardalis.Specification")
            {
                var declaration = (ClassDeclarationSyntax)type.DeclaringSyntaxReferences[0].GetSyntax();
                var diagnostic = Diagnostic.Create(
                    SpecificationDiagnosticDescriptors.LegacyArdalisSpecificationDetected,
                    declaration.Identifier.GetLocation(),
                    type.Name);

                context.ReportDiagnostic(diagnostic);
                // Stryker disable once Statement : Early exit loop optimization
                return;
            }

            baseType = baseType.BaseType;
        }
    }
}


