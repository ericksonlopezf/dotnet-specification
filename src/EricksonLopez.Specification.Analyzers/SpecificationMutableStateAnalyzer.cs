// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EricksonLopez.Specification.Analyzers;

/// <summary>
/// Detects mutable fields and properties in Specification subclasses (SPEC002).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SpecificationMutableStateAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [SpecificationDiagnosticDescriptors.MutableStateInSpecification];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        // Stryker disable once all : Roslyn analyzer lifecycle configuration
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        // Stryker disable once all : Roslyn analyzer lifecycle configuration
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeField, SymbolKind.Field);
        context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Property);
    }

    private static void AnalyzeField(SymbolAnalysisContext context)
    {
        var field = (IFieldSymbol)context.Symbol;

        // Skip constants, readonly, and static fields
        if (field.IsConst || field.IsReadOnly || field.IsStatic)
            return;

        // Only flag fields in Specification subclasses
        if (!field.ContainingType.InheritsFromSpecification())
            return;

        var diagnostic = Diagnostic.Create(
            SpecificationDiagnosticDescriptors.MutableStateInSpecification,
            field.Locations[0],
            field.Name,
            field.ContainingType.Name);

        context.ReportDiagnostic(diagnostic);
    }

    private static void AnalyzeProperty(SymbolAnalysisContext context)
    {
        var property = (IPropertySymbol)context.Symbol;

        // Skip readonly, static, or abstract properties
        if (property.IsReadOnly || property.IsStatic || property.IsAbstract)
            return;

        // Skip properties with private setters (still mutable but less of a concern)
        if (property.SetMethod!.DeclaredAccessibility == Accessibility.Private)
            return;

        // Only flag properties in Specification subclasses
        if (!property.ContainingType.InheritsFromSpecification())
            return;

        var diagnostic = Diagnostic.Create(
            SpecificationDiagnosticDescriptors.MutableStateInSpecification,
            property.Locations[0],
            property.Name,
            property.ContainingType.Name);

        context.ReportDiagnostic(diagnostic);
    }


}

