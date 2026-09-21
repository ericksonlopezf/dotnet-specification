// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace EricksonLopez.Specification.Analyzers;

/// <summary>
/// Provides centralized diagnostic descriptors for all specification analyzers.
/// </summary>
public static class SpecificationDiagnosticDescriptors
{
    private const string Category = "Specification";
    private const string BaseUrl = "https://github.com/ericksonlopezf/dotnet-specification/docs/analyzers/";

    /// <summary>Gets the diagnostic descriptor for SPEC001: Specification class should be sealed or abstract.</summary>
    public static readonly DiagnosticDescriptor SpecificationShouldBeSealedOrAbstract = new(
        id: "SPEC001",
        title: "Specification class should be sealed or abstract",
        messageFormat: "'{0}' inherits from Specification<T> but is neither sealed nor abstract. " +
                       "Specifications should be sealed to prevent unintended inheritance, or abstract if designed as base classes.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Specification classes that are neither sealed nor abstract are an architectural smell. " +
                     "Seal concrete specifications to make them clearly final, or mark them abstract if they are intended as base classes.",
        helpLinkUri: BaseUrl + "SPEC001");

    /// <summary>Gets the diagnostic descriptor for SPEC002: Mutable state in specification.</summary>
    public static readonly DiagnosticDescriptor MutableStateInSpecification = new(
        id: "SPEC002",
        title: "Specification contains mutable state",
        messageFormat: "Field or property '{0}' in specification '{1}' is mutable. " +
                       "Specification instances may be cached; mutable state can cause data corruption or thread-safety issues.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Specifications that capture mutable state are not safely cacheable and can cause " +
                     "thread-safety issues if shared across requests. Consider making fields readonly or using immutable types.",
        helpLinkUri: BaseUrl + "SPEC002");

    /// <summary>Gets the diagnostic descriptor for SPEC003: Expression.Invoke detected in specification expression.</summary>
    public static readonly DiagnosticDescriptor ExpressionInvokeDetected = new(
        id: "SPEC003",
        title: "Expression.Invoke detected in specification expression",
        messageFormat: "'{0}' uses Expression.Invoke which is not supported by many LINQ providers (EF Core, Dapper SQL translation). " +
                       "Use ExpressionComposer.And/Or for provider-safe composition.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Expression.Invoke creates InvocationExpression nodes that EF Core and most SQL translators cannot process. " +
                     "Use the ExpressionComposer helpers which use parameter rebinding instead.",
        helpLinkUri: BaseUrl + "SPEC003");

    /// <summary>Gets the diagnostic descriptor for SPEC004: QuerySpec has no Take limit — potential unbounded query.</summary>
    public static readonly DiagnosticDescriptor UnboundedQuery = new(
        id: "SPEC004",
        title: "QuerySpec has no Take limit — potential unbounded query",
        messageFormat: "QuerySpec<{0}> does not define a Take limit. " +
                       "Queries without limits can return large result sets and cause performance issues.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Consider adding .Take(n) to limit result set size, or suppress this diagnostic if " +
                     "returning all results is intentional (e.g., export scenarios).",
        helpLinkUri: BaseUrl + "SPEC004");

    /// <summary>Gets the diagnostic descriptor for SPEC005: QuerySpec has ordering but no pagination.</summary>
    public static readonly DiagnosticDescriptor OrderingWithoutPagination = new(
        id: "SPEC005",
        title: "QuerySpec has ordering but no pagination",
        messageFormat: "QuerySpec<{0}> defines ordering but no Skip/Take pagination. " +
                       "Ordering large result sets without pagination is expensive.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "When ordering large datasets, consider adding pagination to avoid expensive sort operations " +
                     "on the full dataset.",
        helpLinkUri: BaseUrl + "SPEC005");

    /// <summary>Gets the diagnostic descriptor for SPEC006: Domain specification defined outside Domain layer.</summary>
    public static readonly DiagnosticDescriptor DomainSpecificationOutsideDomain = new(
        id: "SPEC006",
        title: "Domain specification defined outside Domain layer",
        messageFormat: "'{0}' appears to be a domain specification but is defined in '{1}', not a Domain project. " +
                       "Domain specifications should live in the Domain layer.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "Domain specifications should be defined in the Domain project to maintain Clean Architecture boundaries.",
        helpLinkUri: BaseUrl + "SPEC006");

    /// <summary>Gets the diagnostic descriptor for SPEC007: Expression contains non-translatable method call.</summary>
    public static readonly DiagnosticDescriptor PotentialClientSideEvaluation = new(
        id: "SPEC007",
        title: "Expression contains non-translatable method call — potential client-side evaluation",
        messageFormat: "Method '{0}' in specification expression may not be translatable to SQL. " +
                       "Ensure this method is supported by your LINQ provider, or evaluate in memory explicitly.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Method calls inside LINQ expressions may trigger client-side evaluation if the provider " +
                     "does not support them, potentially loading the entire table into memory.",
        helpLinkUri: BaseUrl + "SPEC007");

    /// <summary>Gets the diagnostic descriptor for SPEC008: Infrastructure service injected into domain specification constructor.</summary>
    public static readonly DiagnosticDescriptor InfrastructureServiceInSpecification = new(
        id: "SPEC008",
        title: "Infrastructure service injected into domain specification constructor",
        messageFormat: "Constructor parameter '{0}' of type '{1}' in specification '{2}' appears to be an infrastructure service. " +
                       "Domain specifications should depend only on value types and domain objects, not services.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Injecting infrastructure services (repositories, DbContext, HTTP clients, etc.) into a domain specification " +
                     "violates the Dependency Inversion Principle and prevents in-memory evaluation without infrastructure. " +
                     "Pass only primitive values or domain objects required to define the predicate.",
        helpLinkUri: BaseUrl + "SPEC008");

    /// <summary>Gets the diagnostic descriptor for SPEC009: Async lambda or await expression inside BuildExpression.</summary>
    public static readonly DiagnosticDescriptor AsyncLambdaInExpression = new(
        id: "SPEC009",
        title: "Async lambda or await expression inside BuildExpression",
        messageFormat: "'{0}' contains an async lambda or await expression inside BuildExpression(). " +
                       "LINQ expression trees cannot represent async operations; IQueryable providers will throw at runtime.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Expression trees (Expression<Func<T, bool>>) cannot contain async/await operations. " +
                     "Remove async lambdas from BuildExpression(). If async data is needed, resolve it before constructing the specification.",
        helpLinkUri: BaseUrl + "SPEC009");

    /// <summary>Gets the diagnostic descriptor for SPEC010: IsSatisfiedBy called inside BuildExpression.</summary>
    public static readonly DiagnosticDescriptor IsSatisfiedByInsideBuildExpression = new(
        id: "SPEC010",
        title: "IsSatisfiedBy called inside BuildExpression",
        messageFormat: "'{0}' calls IsSatisfiedBy() inside BuildExpression(). " +
                       "IsSatisfiedBy evaluates expressions and cannot be part of an expression tree intended for SQL translation.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Calling IsSatisfiedBy() inside BuildExpression() creates a closure over the evaluator, not an expression tree. " +
                     "Use And() / Or() composition methods instead to combine specification logic at the expression tree level.",
        helpLinkUri: BaseUrl + "SPEC010");

    /// <summary>Gets the diagnostic descriptor for SPEC011: Legacy Ardalis.Specification usage detected.</summary>
    public static readonly DiagnosticDescriptor LegacyArdalisSpecificationDetected = new(
        id: "SPEC011",
        title: "Legacy Ardalis.Specification usage detected",
        messageFormat: "'{0}' inherits from Ardalis.Specification. Consider migrating to EricksonLopez.Specification for pure DDD and zero-allocation query plans.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Ardalis.Specification mixes domain specifications with infrastructure concerns like Include and mutable query state. " +
                     "Migrating to EricksonLopez.Specification provides pure domain expressions and immutable QuerySpec descriptors.",
        helpLinkUri: BaseUrl + "SPEC011");
}



