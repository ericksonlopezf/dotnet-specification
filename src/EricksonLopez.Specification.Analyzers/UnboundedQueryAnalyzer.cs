// Copyright © Erickson Lopez. MIT License.
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EricksonLopez.Specification.Analyzers;

/// <summary>
/// Detects <c>QuerySpec&lt;T&gt;</c> chains that define no <c>Take</c> or <c>Page</c> limit (SPEC004).
/// </summary>
/// <remarks>
/// Queries without row limits may return large result sets and cause performance issues.
/// This diagnostic fires when a <c>QuerySpec&lt;T&gt;</c> is produced without any call to
/// <c>.Take()</c> or <c>.Page()</c> in the same fluent chain.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UnboundedQueryAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [SpecificationDiagnosticDescriptors.UnboundedQuery];

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

            var typeInfo = context.SemanticModel.GetTypeInfo(variable.Initializer.Value);
            if (!IsQuerySpecType(typeInfo.Type))
                continue;

            // Check if the fluent chain includes a Take or Page call
            if (!HasBoundingCall(variable.Initializer.Value))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    SpecificationDiagnosticDescriptors.UnboundedQuery,
                    variable.GetLocation(),
                    GetEntityTypeName(typeInfo.Type)));
            }
        }
    }

    private static bool IsQuerySpecType(ITypeSymbol? type)
    {
        if (type is null) return false;
        return type.Name == "QuerySpec" &&
               type.ContainingNamespace?.ToString() == "EricksonLopez.Specification";
    }

    private static bool HasBoundingCall(ExpressionSyntax expression)
    {
        foreach (var invocation in expression.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                var name = memberAccess.Name.Identifier.Text;
                if (name is "Take" or "Page" or "SeekAfter" or "SeekBefore" or "WithCursor")
                    return true;
            }
        }
        return false;
    }

    private static string GetEntityTypeName(ITypeSymbol? type)
    {
        if (type is INamedTypeSymbol named && named.TypeArguments.Length > 0)
            return named.TypeArguments[0].Name;
        return type?.Name ?? "T";
    }
}
