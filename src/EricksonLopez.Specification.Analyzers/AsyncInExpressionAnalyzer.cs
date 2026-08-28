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
/// Detects async lambdas or await expressions inside <c>BuildExpression()</c>
/// in Specification subclasses (SPEC009).
/// </summary>
/// <remarks>
/// Expression trees cannot represent async/await operations. Placing them inside
/// <c>BuildExpression()</c> causes IQueryable providers to throw at runtime.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AsyncInExpressionAnalyzer : DiagnosticAnalyzer
{
    private const string BuildExpressionName = "BuildExpression";

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [SpecificationDiagnosticDescriptors.AsyncLambdaInExpression];

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

        var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDecl)!;

        // Only process methods on Specification<T> subclasses
        if (!methodSymbol.ContainingType.InheritsFromSpecification())
            return;

        // Walk the method body looking for:
        //   1. Async lambda expressions (async () => ..., async x => ...)
        //   2. Await expressions
        foreach (var node in methodDecl.DescendantNodes())
        {
            Location? reportLocation = null;

            switch (node)
            {
                case LambdaExpressionSyntax lambda when lambda.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword):
                    reportLocation = lambda.AsyncKeyword.GetLocation();
                    break;

                case AwaitExpressionSyntax awaitExpr:
                    reportLocation = awaitExpr.AwaitKeyword.GetLocation();
                    break;
            }

            if (reportLocation is null)
                continue;

            var diagnostic = Diagnostic.Create(
                SpecificationDiagnosticDescriptors.AsyncLambdaInExpression,
                reportLocation,
                methodSymbol.ContainingType.Name);

            context.ReportDiagnostic(diagnostic);

            // Report once per method — one diagnostic is enough to guide the developer
            return;
        }
    }
}

