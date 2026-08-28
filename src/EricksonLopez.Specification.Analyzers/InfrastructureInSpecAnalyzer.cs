// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EricksonLopez.Specification.Analyzers;

/// <summary>
/// Detects infrastructure services (IServiceProvider, DbContext, repositories, clients, etc.)
/// injected into the constructor of a Specification subclass (SPEC008).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class InfrastructureInSpecAnalyzer : DiagnosticAnalyzer
{
    // Well-known infrastructure type names (exact)
    private static readonly ImmutableHashSet<string> KnownInfraTypeNames = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "ILogger");

    // Suspicious type-name suffixes that indicate infrastructure
    private static readonly ImmutableArray<string> InfraSuffixes = ImmutableArray.Create(
        "Repository",
        "Service",
        "Context",
        "Client",
        "Manager",
        "Provider",
        "Factory",
        "Sender",
        "Dispatcher",
        "Bus",
        "Store",
        "Cache",
        "Dao");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [SpecificationDiagnosticDescriptors.InfrastructureServiceInSpecification];

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

        if (!type.InheritsFromSpecification())
            return;

        // Check each constructor for infrastructure parameters
        foreach (var ctor in type.Constructors)
        {

            foreach (var parameter in ctor.Parameters)
            {
                if (!IsInfrastructureType(parameter.Type))
                    continue;

                var location = parameter.Locations[0];

                var diagnostic = Diagnostic.Create(
                    SpecificationDiagnosticDescriptors.InfrastructureServiceInSpecification,
                    location,
                    parameter.Name,
                    parameter.Type.Name,
                    type.Name);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private static bool IsInfrastructureType(ITypeSymbol typeSymbol)
    {
        var name = typeSymbol.Name;

        if (KnownInfraTypeNames.Contains(name))
            return true;

        foreach (var suffix in InfraSuffixes)
        {
            if (name.EndsWith(suffix, StringComparison.Ordinal))
                return true;
        }

        // Also check the base type chain for DbContext
        var current = typeSymbol as INamedTypeSymbol;
        while (current is not null)
        {
            if (current.Name == "DbContext" &&
                current.ContainingNamespace!.ToString() == "Microsoft.EntityFrameworkCore")
                return true;

            current = current.BaseType;
        }

        return false;
    }
}


