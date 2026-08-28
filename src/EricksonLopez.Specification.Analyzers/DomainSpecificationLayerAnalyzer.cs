// Copyright © Erickson Lopez. MIT License.
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EricksonLopez.Specification.Analyzers;

/// <summary>
/// Detects domain <c>Specification&lt;T&gt;</c> subclasses declared in Infrastructure namespaces (SPEC006).
/// </summary>
/// <remarks>
/// Domain specifications should live in the Domain layer. This analyzer flags any class
/// inheriting from <c>Specification&lt;T&gt;</c> whose containing namespace contains tokens
/// typically associated with the Infrastructure layer (e.g., <c>Infrastructure</c>,
/// <c>Persistence</c>, <c>Data</c>, <c>Repository</c>, <c>EntityFramework</c>, <c>EfCore</c>).
/// This rule is off by default (<c>isEnabledByDefault: false</c>) and can be enabled
/// via <c>.editorconfig</c> when strict Clean Architecture enforcement is desired.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DomainSpecificationLayerAnalyzer : DiagnosticAnalyzer
{
    private static readonly string[] InfrastructureNamespaceTokens =
    [
        "Infrastructure",
        "Persistence",
        "Repository",
        "Repositories",
        "EntityFramework",
        "EfCore",
        "SqlServer",
        "Dapper",
        "MongoDB",
        "Data"
    ];

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [SpecificationDiagnosticDescriptors.DomainSpecificationOutsideDomain];

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

        var namespaceName = type.ContainingNamespace?.ToString() ?? string.Empty;
        // Stryker disable once Equality : Token at start of namespace (index 0) must match
        var matchedToken = InfrastructureNamespaceTokens
            .FirstOrDefault(token => namespaceName.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0);

        if (matchedToken is null)
            return;

        // Stryker disable once Linq : Empty DeclaringSyntaxReferences safe access
        var declaration = type.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(context.CancellationToken);
        var location = declaration is ClassDeclarationSyntax classDecl
            ? classDecl.Identifier.GetLocation()
            : declaration?.GetLocation() ?? type.Locations[0];

        context.ReportDiagnostic(Diagnostic.Create(
            SpecificationDiagnosticDescriptors.DomainSpecificationOutsideDomain,
            location,
            type.Name,
            namespaceName));
    }
}
