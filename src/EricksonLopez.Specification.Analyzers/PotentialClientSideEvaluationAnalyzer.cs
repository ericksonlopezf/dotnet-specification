// Copyright © Erickson Lopez. MIT License.
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EricksonLopez.Specification.Analyzers;

/// <summary>
/// Detects method calls inside <c>BuildExpression()</c> that are likely non-translatable by
/// IQueryable providers such as EF Core (SPEC007).
/// </summary>
/// <remarks>
/// Only a subset of .NET methods can be translated to SQL by LINQ providers. Method calls that
/// have no known translation will typically cause client-side evaluation or runtime exceptions.
/// This analyzer warns when a non-standard (non-BCL allowlisted) method is invoked inside
/// a specification's <c>BuildExpression()</c> body.
/// Known safe methods include: <c>string.Contains</c>, <c>string.StartsWith</c>,
/// <c>string.EndsWith</c>, <c>string.IsNullOrEmpty</c>, <c>string.IsNullOrWhiteSpace</c>,
/// <c>EF.Functions.*</c>, comparison operators, and member access.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PotentialClientSideEvaluationAnalyzer : DiagnosticAnalyzer
{
    private const string BuildExpressionName = "BuildExpression";

    // Stryker disable String : Allowlist of known translatable method names
    // Known method names that most IQueryable providers (EF Core) can translate to SQL
    private static readonly ImmutableHashSet<string> KnownTranslatableMethods = ImmutableHashSet.Create(
        System.StringComparer.Ordinal,
        // String methods
        "Contains", "StartsWith", "EndsWith", "ToLower", "ToUpper", "Trim", "TrimStart", "TrimEnd",
        "IsNullOrEmpty", "IsNullOrWhiteSpace", "Substring", "Replace", "IndexOf", "Length",
        // Math methods
        "Abs", "Ceiling", "Floor", "Round", "Pow", "Sqrt",
        // Enumerable / collection
        "Any", "All", "Count",
        // EF.Functions methods (any method on EF.Functions is considered translatable)
        "Like",
        // Comparison and null handling
        "GetValueOrDefault", "HasValue",
        // Date / time
        "AddDays", "AddMonths", "AddYears", "AddHours", "AddMinutes", "AddSeconds",
        // Coalesce / null check
        "Equals"
    );
    // Stryker restore String

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [SpecificationDiagnosticDescriptors.PotentialClientSideEvaluation];

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

        // Stryker disable once Equality,Statement : Method name guard
        if (methodDecl.Identifier.Text != BuildExpressionName)
            return;

        var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDecl, context.CancellationToken);
        if (methodSymbol is null || !methodSymbol.ContainingType.InheritsFromSpecification())
            return;

        // Walk looking for method call expressions inside lambdas (the expression tree body)
        foreach (var lambda in methodDecl.DescendantNodes().OfType<LambdaExpressionSyntax>())
        {
            foreach (var invocation in lambda.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                var invokedSymbol = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol as IMethodSymbol;
                if (invokedSymbol is null)
                    continue;

                // Skip known-translatable methods
                if (KnownTranslatableMethods.Contains(invokedSymbol.Name))
                    continue;

                // Stryker disable once Statement : EF.Functions method invocation skip
                // Skip EF.Functions methods (any method accessed from EF.Functions property)
                if (IsEfFunctionsMethod(invocation))
                    continue;

                // Stryker disable once Equality,Logical,Statement : Operator check
                if (invokedSymbol.MethodKind == MethodKind.UserDefinedOperator ||
                    invokedSymbol.MethodKind == MethodKind.BuiltinOperator)
                    continue;

                // Stryker disable once Equality,Statement : Constructor calls skip
                if (invokedSymbol.MethodKind == MethodKind.Constructor)
                    continue;

                // Only warn about non-BCL types; BCL types are generally safe
                var containingAssembly = invokedSymbol.ContainingAssembly?.Name;
                // Stryker disable once String,Logical : BCL assembly name check
                bool isBcl = containingAssembly is "System.Runtime" or "System.Private.CoreLib"
                    or "System.Core" or "netstandard" or "mscorlib";

                if (!isBcl)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        SpecificationDiagnosticDescriptors.PotentialClientSideEvaluation,
                        invocation.GetLocation(),
                        invokedSymbol.Name));
                    // Stryker disable once Statement : Early exit after first diagnostic
                    return; // One diagnostic per BuildExpression is sufficient
                }
            }
        }
    }

    private static bool IsEfFunctionsMethod(InvocationExpressionSyntax invocation) =>
        invocation.Expression is MemberAccessExpressionSyntax
        {
            Expression: MemberAccessExpressionSyntax { Name.Identifier.Text: "Functions" }
        };
}
