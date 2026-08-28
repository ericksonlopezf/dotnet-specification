// Copyright © Erickson Lopez. MIT License.
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EricksonLopez.Specification.Analyzers;

/// <summary>
/// Detects <c>QuerySpec&lt;T&gt;</c> chains that apply ordering without pagination (SPEC005).
/// </summary>
/// <remarks>
/// Sorting large datasets without a row limit can be expensive. This diagnostic fires
/// when a <c>QuerySpec&lt;T&gt;</c> chain includes <c>OrderBy</c>/<c>OrderByDescending</c>
/// but no corresponding <c>Take</c>, <c>Page</c>, or cursor-based pagination.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class OrderingWithoutPaginationAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [SpecificationDiagnosticDescriptors.OrderingWithoutPagination];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        // Stryker disable once all : Roslyn analyzer lifecycle configuration
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        // Stryker disable once all : Roslyn analyzer lifecycle configuration
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeVariableDeclaration, SyntaxKind.LocalDeclarationStatement);
    }

    private static void AnalyzeVariableDeclaration(SyntaxNodeAnalysisContext context)
    {
        var localDecl = (LocalDeclarationStatementSyntax)context.Node;
        foreach (var variable in localDecl.Declaration.Variables)
        {
            if (variable.Initializer?.Value is null)
                continue;

            var typeInfo = context.SemanticModel.GetTypeInfo(variable.Initializer.Value, context.CancellationToken);
            if (!IsQuerySpecType(typeInfo.Type))
                continue;

            bool hasOrdering = HasOrderingCall(variable.Initializer.Value);
            bool hasPagination = HasPaginationCall(variable.Initializer.Value);

            if (hasOrdering && !hasPagination)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    SpecificationDiagnosticDescriptors.OrderingWithoutPagination,
                    variable.Identifier.GetLocation(),
                    GetEntityTypeName(typeInfo.Type)));
            }
        }
    }

    private static bool IsQuerySpecType(ITypeSymbol? type)
    {
        if (type is null) return false;
        // Stryker disable once Logical,Equality : QuerySpec type identity
        return type.Name == "QuerySpec" &&
               type.ContainingNamespace?.ToString() == "EricksonLopez.Specification";
    }

    private static bool HasOrderingCall(ExpressionSyntax expression)
    {
        foreach (var invocation in expression.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is MemberAccessExpressionSyntax ma)
            {
                var name = ma.Name.Identifier.Text;
                // Stryker disable once String,Logical : Method name matching
                if (name is "OrderBy" or "OrderByDescending" or "ThenBy" or "ThenByDescending")
                    return true;
            }
        }
        return false;
    }

    private static bool HasPaginationCall(ExpressionSyntax expression)
    {
        foreach (var invocation in expression.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is MemberAccessExpressionSyntax ma)
            {
                var name = ma.Name.Identifier.Text;
                // Stryker disable once String,Logical : Pagination method name matching
                if (name is "Take" or "Page" or "SeekAfter" or "SeekBefore" or "WithCursor")
                    return true;
            }
        }
        return false;
    }

    private static string GetEntityTypeName(ITypeSymbol? type) =>
        type is INamedTypeSymbol { TypeArguments.Length: > 0 } named
            ? named.TypeArguments[0].Name
            : "T";
}
