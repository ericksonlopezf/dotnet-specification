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
/// Detects calls to <c>IsSatisfiedBy()</c> inside <c>BuildExpression()</c>
/// in Specification subclasses (SPEC010).
/// </summary>
/// <remarks>
/// <c>IsSatisfiedBy()</c> evaluates expressions by running the interpreter, producing
/// a closure — not an expression tree node. Calling it inside <c>BuildExpression()</c>
/// creates an expression tree that cannot be translated to SQL and will throw at runtime
/// when passed to IQueryable providers.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class IsSatisfiedByInExpressionAnalyzer : DiagnosticAnalyzer
{
    private const string BuildExpressionName = "BuildExpression";
    private const string IsSatisfiedByName = "IsSatisfiedBy";

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [SpecificationDiagnosticDescriptors.IsSatisfiedByInsideBuildExpression];

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

        // Walk the method body looking for invocations of IsSatisfiedBy(...)
        foreach (var invocationNode in methodDecl.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            // Check if the invocation is a call to a method named IsSatisfiedBy
            var memberName = invocationNode.Expression switch
            {
                MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
                IdentifierNameSyntax identifier => identifier.Identifier.Text,
                _ => null
            };

            if (memberName != IsSatisfiedByName)
                continue;

            // Optionally verify via SemanticModel that it's actually the spec's IsSatisfiedBy
            // For robustness we match by name only (avoids false negatives from unresolved symbols)
            var diagnostic = Diagnostic.Create(
                SpecificationDiagnosticDescriptors.IsSatisfiedByInsideBuildExpression,
                invocationNode.GetLocation(),
                methodSymbol.ContainingType.Name);

            context.ReportDiagnostic(diagnostic);

            // One diagnostic per method is sufficient
            return;
        }
    }


}

