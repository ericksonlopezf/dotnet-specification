// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EricksonLopez.Specification.Analyzers;

/// <summary>
/// Detects <c>Expression.Invoke</c> calls inside <c>BuildExpression()</c>
/// in Specification subclasses (SPEC003).
/// </summary>
/// <remarks>
/// <c>Expression.Invoke</c> creates <c>InvocationExpression</c> nodes that most LINQ providers
/// (EF Core, SQL translators) cannot process. Use <c>ExpressionComposer.And/Or</c> instead,
/// which rewrites parameter references via <c>ParameterReplacer</c>.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ExpressionInvokeAnalyzer : DiagnosticAnalyzer
{
    private const string BuildExpressionName = "BuildExpression";
    private const string ExpressionInvokeName = "Invoke";

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [SpecificationDiagnosticDescriptors.ExpressionInvokeDetected];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        // Stryker disable once all : Roslyn analyzer lifecycle configuration
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        // Stryker disable once all : Roslyn analyzer lifecycle configuration
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
    {
        var methodDecl = (MethodDeclarationSyntax)context.Node;

        // Only process BuildExpression() methods
        if (methodDecl.Identifier.Text != BuildExpressionName)
            return;

        var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDecl, context.CancellationToken);
        if (methodSymbol is null || !methodSymbol.ContainingType.InheritsFromSpecification())
            return;

        // Walk the method body looking for Expression.Invoke(...)
        foreach (var node in methodDecl.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (node.Expression is not MemberAccessExpressionSyntax memberAccess)
                continue;

            if (memberAccess.Name.Identifier.Text != ExpressionInvokeName)
                continue;

            // Verify the receiver resolves to System.Linq.Expressions.Expression (static class)
            var receiverSymbol = context.SemanticModel.GetSymbolInfo(memberAccess.Expression, context.CancellationToken).Symbol;
            bool isStaticExpressionInvoke = receiverSymbol is INamedTypeSymbol typeSymbol &&
                typeSymbol.Name == "Expression" &&
                typeSymbol.ContainingNamespace?.ToString() == "System.Linq.Expressions";

            if (!isStaticExpressionInvoke)
                continue;

            var diagnostic = Diagnostic.Create(
                SpecificationDiagnosticDescriptors.ExpressionInvokeDetected,
                node.GetLocation(),
                methodSymbol.ContainingType.Name);

            context.ReportDiagnostic(diagnostic);
            return; // Report once per method — one diagnostic guides the developer
        }
    }
}
