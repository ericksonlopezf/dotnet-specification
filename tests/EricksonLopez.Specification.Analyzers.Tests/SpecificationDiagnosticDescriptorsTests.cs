// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Threading;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Xunit;

namespace EricksonLopez.Specification.Analyzers.Tests;

public sealed class SpecificationDiagnosticDescriptorsTests
{
    [Fact]
    public void Spec001_HasCorrectProperties()
    {
        var d = SpecificationDiagnosticDescriptors.SpecificationShouldBeSealedOrAbstract;
        d.Id.Should().Be("SPEC001");
        d.DefaultSeverity.Should().Be(DiagnosticSeverity.Warning);
        d.Title.ToString().Should().Be("Specification class should be sealed or abstract");
        d.MessageFormat.ToString().Should().Be("'{0}' inherits from Specification<T> but is neither sealed nor abstract. Specifications should be sealed to prevent unintended inheritance, or abstract if designed as base classes.");
        d.Category.Should().Be("Specification");
        d.IsEnabledByDefault.Should().BeTrue();
        d.Description.ToString().Should().Be("Specification classes that are neither sealed nor abstract are an architectural smell. Seal concrete specifications to make them clearly final, or mark them abstract if they are intended as base classes.");
        d.HelpLinkUri.Should().Be("https://github.com/ericksonlopezf/dotnet-specification/docs/analyzers/SPEC001");
    }

    [Fact]
    public void Spec002_HasCorrectProperties()
    {
        var d = SpecificationDiagnosticDescriptors.MutableStateInSpecification;
        d.Id.Should().Be("SPEC002");
        d.DefaultSeverity.Should().Be(DiagnosticSeverity.Warning);
        d.Title.ToString().Should().Be("Specification contains mutable state");
        d.MessageFormat.ToString().Should().Be("Field or property '{0}' in specification '{1}' is mutable. Specification instances may be cached; mutable state can cause data corruption or thread-safety issues.");
        d.Category.Should().Be("Specification");
        d.IsEnabledByDefault.Should().BeTrue();
        d.Description.ToString().Should().Be("Specifications that capture mutable state are not safely cacheable and can cause thread-safety issues if shared across requests. Consider making fields readonly or using immutable types.");
        d.HelpLinkUri.Should().Be("https://github.com/ericksonlopezf/dotnet-specification/docs/analyzers/SPEC002");
    }

    [Fact]
    public void Spec003_HasCorrectProperties()
    {
        var d = SpecificationDiagnosticDescriptors.ExpressionInvokeDetected;
        d.Id.Should().Be("SPEC003");
        d.DefaultSeverity.Should().Be(DiagnosticSeverity.Error);
        d.Title.ToString().Should().Be("Expression.Invoke detected in specification expression");
        d.MessageFormat.ToString().Should().Be("'{0}' uses Expression.Invoke which is not supported by many LINQ providers (EF Core, Dapper SQL translation). Use ExpressionComposer.And/Or for provider-safe composition.");
        d.Category.Should().Be("Specification");
        d.IsEnabledByDefault.Should().BeTrue();
        d.Description.ToString().Should().Be("Expression.Invoke creates InvocationExpression nodes that EF Core and most SQL translators cannot process. Use the ExpressionComposer helpers which use parameter rebinding instead.");
        d.HelpLinkUri.Should().Be("https://github.com/ericksonlopezf/dotnet-specification/docs/analyzers/SPEC003");
    }

    [Fact]
    public void Spec004_HasCorrectProperties()
    {
        var d = SpecificationDiagnosticDescriptors.UnboundedQuery;
        d.Id.Should().Be("SPEC004");
        d.DefaultSeverity.Should().Be(DiagnosticSeverity.Info);
        d.Title.ToString().Should().Be("QuerySpec has no Take limit — potential unbounded query");
        d.MessageFormat.ToString().Should().Be("QuerySpec<{0}> does not define a Take limit. Queries without limits can return large result sets and cause performance issues.");
        d.Category.Should().Be("Specification");
        d.IsEnabledByDefault.Should().BeTrue();
        d.Description.ToString().Should().Be("Consider adding .Take(n) to limit result set size, or suppress this diagnostic if returning all results is intentional (e.g., export scenarios).");
        d.HelpLinkUri.Should().Be("https://github.com/ericksonlopezf/dotnet-specification/docs/analyzers/SPEC004");
    }

    [Fact]
    public void Spec005_HasCorrectProperties()
    {
        var d = SpecificationDiagnosticDescriptors.OrderingWithoutPagination;
        d.Id.Should().Be("SPEC005");
        d.DefaultSeverity.Should().Be(DiagnosticSeverity.Info);
        d.Title.ToString().Should().Be("QuerySpec has ordering but no pagination");
        d.MessageFormat.ToString().Should().Be("QuerySpec<{0}> defines ordering but no Skip/Take pagination. Ordering large result sets without pagination is expensive.");
        d.Category.Should().Be("Specification");
        d.IsEnabledByDefault.Should().BeFalse();
        d.Description.ToString().Should().Be("When ordering large datasets, consider adding pagination to avoid expensive sort operations on the full dataset.");
        d.HelpLinkUri.Should().Be("https://github.com/ericksonlopezf/dotnet-specification/docs/analyzers/SPEC005");
    }

    [Fact]
    public void Spec006_HasCorrectProperties()
    {
        var d = SpecificationDiagnosticDescriptors.DomainSpecificationOutsideDomain;
        d.Id.Should().Be("SPEC006");
        d.DefaultSeverity.Should().Be(DiagnosticSeverity.Info);
        d.Title.ToString().Should().Be("Domain specification defined outside Domain layer");
        d.MessageFormat.ToString().Should().Be("'{0}' appears to be a domain specification but is defined in '{1}', not a Domain project. Domain specifications should live in the Domain layer.");
        d.Category.Should().Be("Specification");
        d.IsEnabledByDefault.Should().BeFalse();
        d.Description.ToString().Should().Be("Domain specifications should be defined in the Domain project to maintain Clean Architecture boundaries.");
        d.HelpLinkUri.Should().Be("https://github.com/ericksonlopezf/dotnet-specification/docs/analyzers/SPEC006");
    }

    [Fact]
    public void Spec007_HasCorrectProperties()
    {
        var d = SpecificationDiagnosticDescriptors.PotentialClientSideEvaluation;
        d.Id.Should().Be("SPEC007");
        d.DefaultSeverity.Should().Be(DiagnosticSeverity.Warning);
        d.Title.ToString().Should().Be("Expression contains non-translatable method call — potential client-side evaluation");
        d.MessageFormat.ToString().Should().Be("Method '{0}' in specification expression may not be translatable to SQL. Ensure this method is supported by your LINQ provider, or evaluate in memory explicitly.");
        d.Category.Should().Be("Specification");
        d.IsEnabledByDefault.Should().BeTrue();
        d.Description.ToString().Should().Be("Method calls inside LINQ expressions may trigger client-side evaluation if the provider does not support them, potentially loading the entire table into memory.");
        d.HelpLinkUri.Should().Be("https://github.com/ericksonlopezf/dotnet-specification/docs/analyzers/SPEC007");
    }

    [Fact]
    public void Spec008_HasCorrectProperties()
    {
        var d = SpecificationDiagnosticDescriptors.InfrastructureServiceInSpecification;
        d.Id.Should().Be("SPEC008");
        d.DefaultSeverity.Should().Be(DiagnosticSeverity.Warning);
        d.Title.ToString().Should().Be("Infrastructure service injected into domain specification constructor");
        d.MessageFormat.ToString().Should().Be("Constructor parameter '{0}' of type '{1}' in specification '{2}' appears to be an infrastructure service. Domain specifications should depend only on value types and domain objects, not services.");
        d.Category.Should().Be("Specification");
        d.IsEnabledByDefault.Should().BeTrue();
        d.Description.ToString().Should().Be("Injecting infrastructure services (repositories, DbContext, HTTP clients, etc.) into a domain specification violates the Dependency Inversion Principle and prevents in-memory evaluation without infrastructure. Pass only primitive values or domain objects required to define the predicate.");
        d.HelpLinkUri.Should().Be("https://github.com/ericksonlopezf/dotnet-specification/docs/analyzers/SPEC008");
    }

    [Fact]
    public void Spec009_HasCorrectProperties()
    {
        var d = SpecificationDiagnosticDescriptors.AsyncLambdaInExpression;
        d.Id.Should().Be("SPEC009");
        d.DefaultSeverity.Should().Be(DiagnosticSeverity.Error);
        d.Title.ToString().Should().Be("Async lambda or await expression inside BuildExpression");
        d.MessageFormat.ToString().Should().Be("'{0}' contains an async lambda or await expression inside BuildExpression(). LINQ expression trees cannot represent async operations; IQueryable providers will throw at runtime.");
        d.Category.Should().Be("Specification");
        d.IsEnabledByDefault.Should().BeTrue();
        d.Description.ToString().Should().Be("Expression trees (Expression<Func<T, bool>>) cannot contain async/await operations. Remove async lambdas from BuildExpression(). If async data is needed, resolve it before constructing the specification.");
        d.HelpLinkUri.Should().Be("https://github.com/ericksonlopezf/dotnet-specification/docs/analyzers/SPEC009");
    }

    [Fact]
    public void Spec010_HasCorrectProperties()
    {
        var d = SpecificationDiagnosticDescriptors.IsSatisfiedByInsideBuildExpression;
        d.Id.Should().Be("SPEC010");
        d.DefaultSeverity.Should().Be(DiagnosticSeverity.Error);
        d.Title.ToString().Should().Be("IsSatisfiedBy called inside BuildExpression");
        d.MessageFormat.ToString().Should().Be("'{0}' calls IsSatisfiedBy() inside BuildExpression(). IsSatisfiedBy evaluates expressions and cannot be part of an expression tree intended for SQL translation.");
        d.Category.Should().Be("Specification");
        d.IsEnabledByDefault.Should().BeTrue();
        d.Description.ToString().Should().Be("Calling IsSatisfiedBy() inside BuildExpression() creates a closure over the evaluator, not an expression tree. Use And() / Or() composition methods instead to combine specification logic at the expression tree level.");
        d.HelpLinkUri.Should().Be("https://github.com/ericksonlopezf/dotnet-specification/docs/analyzers/SPEC010");
    }

    [Fact]
    public void Spec011_HasCorrectProperties()
    {
        var d = SpecificationDiagnosticDescriptors.LegacyArdalisSpecificationDetected;
        d.Id.Should().Be("SPEC011");
        d.DefaultSeverity.Should().Be(DiagnosticSeverity.Info);
        d.Title.ToString().Should().Be("Legacy Ardalis.Specification usage detected");
        d.MessageFormat.ToString().Should().Be("'{0}' inherits from Ardalis.Specification. Consider migrating to EricksonLopez.Specification for pure DDD and zero-allocation query plans.");
        d.Category.Should().Be("Specification");
        d.IsEnabledByDefault.Should().BeTrue();
        d.Description.ToString().Should().Be("Ardalis.Specification mixes domain specifications with infrastructure concerns like Include and mutable query state. Migrating to EricksonLopez.Specification provides pure domain expressions and immutable QuerySpec descriptors.");
        d.HelpLinkUri.Should().Be("https://github.com/ericksonlopezf/dotnet-specification/docs/analyzers/SPEC011");
    }
}




