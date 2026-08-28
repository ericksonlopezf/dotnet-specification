// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Linq.Expressions;
using AwesomeAssertions;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Xunit;

namespace EricksonLopez.Specification.Tests;

/// <summary>
/// Property-based tests for specification algebra laws.
/// Uses FsCheck to verify that Boolean algebra laws hold for arbitrary customer data,
/// not just a fixed set of hand-written examples.
/// </summary>
/// <remarks>
/// FsCheck [Property] tests (200 random inputs each) verify:
///   - Identity:         A AND TRUE = A, A OR FALSE = A
///   - Annihilator:      A AND FALSE = FALSE, A OR TRUE = TRUE
///   - Double negation:  NOT(NOT(A)) = A
///   - Commutativity:    A AND B = B AND A, A OR B = B OR A
///   - De Morgan's:      NOT(A AND B) = NOT(A) OR NOT(B), NOT(A OR B) = NOT(A) AND NOT(B)
///
/// Deterministic [Fact] regression tests verify the same laws against a fixed customer set.
/// </remarks>
public sealed class SpecificationAlgebraTests
{
    // ──────────────────────────────────────────────────────────────────────────
    // Property-based: Identity laws (FsCheck — arbitrary inputs)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>A AND TRUE = A for any customer.</summary>
    [Property(Arbitrary = [typeof(CustomerArbitraryFactory)], MaxTest = 200)]
    public bool AndIdentity_AAndTrue_EquivalentToA_ForAnyCustomer(Customer customer)
    {
        var spec = new ActiveCustomerSpecification();
        var composite = spec.And(Spec.True<Customer>());
        return spec.IsSatisfiedBy(customer) == composite.IsSatisfiedBy(customer);
    }

    /// <summary>A OR FALSE = A for any customer.</summary>
    [Property(Arbitrary = [typeof(CustomerArbitraryFactory)], MaxTest = 200)]
    public bool OrIdentity_AOrFalse_EquivalentToA_ForAnyCustomer(Customer customer)
    {
        var spec = new ActiveCustomerSpecification();
        var composite = spec.Or(Spec.False<Customer>());
        return spec.IsSatisfiedBy(customer) == composite.IsSatisfiedBy(customer);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Property-based: Annihilator laws (FsCheck — arbitrary inputs)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>A AND FALSE = FALSE for any customer.</summary>
    [Property(Arbitrary = [typeof(CustomerArbitraryFactory)], MaxTest = 200)]
    public bool AndAnnihilator_AAndFalse_AlwaysFalse_ForAnyCustomer(Customer customer)
    {
        var spec = new ActiveCustomerSpecification().And(Spec.False<Customer>());
        return !spec.IsSatisfiedBy(customer);
    }

    /// <summary>A OR TRUE = TRUE for any customer.</summary>
    [Property(Arbitrary = [typeof(CustomerArbitraryFactory)], MaxTest = 200)]
    public bool OrAnnihilator_AOrTrue_AlwaysTrue_ForAnyCustomer(Customer customer)
    {
        var spec = new ActiveCustomerSpecification().Or(Spec.True<Customer>());
        return spec.IsSatisfiedBy(customer);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Property-based: Double negation (FsCheck — arbitrary inputs)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>NOT(NOT(A)) = A for any customer.</summary>
    [Property(Arbitrary = [typeof(CustomerArbitraryFactory)], MaxTest = 200)]
    public bool DoubleNegation_NotNotA_EquivalentToA_ForAnyCustomer(Customer customer)
    {
        var spec = new ActiveCustomerSpecification();
        var doubleNegated = spec.Not().Not();
        return spec.IsSatisfiedBy(customer) == doubleNegated.IsSatisfiedBy(customer);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Property-based: Commutativity (FsCheck — arbitrary inputs)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>A AND B = B AND A for any customer.</summary>
    [Property(Arbitrary = [typeof(CustomerArbitraryFactory)], MaxTest = 200)]
    public bool AndCommutativity_AAndB_EqualsBAndA_ForAnyCustomer(Customer customer)
    {
        var specA = new ActiveCustomerSpecification();
        var specB = new NotDeletedCustomerSpecification();
        return specA.And(specB).IsSatisfiedBy(customer) == specB.And(specA).IsSatisfiedBy(customer);
    }

    /// <summary>A OR B = B OR A for any customer.</summary>
    [Property(Arbitrary = [typeof(CustomerArbitraryFactory)], MaxTest = 200)]
    public bool OrCommutativity_AOrB_EqualsBOrA_ForAnyCustomer(Customer customer)
    {
        var specA = new ActiveCustomerSpecification();
        var specB = new NotDeletedCustomerSpecification();
        return specA.Or(specB).IsSatisfiedBy(customer) == specB.Or(specA).IsSatisfiedBy(customer);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Property-based: De Morgan's laws (FsCheck — arbitrary inputs)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>NOT(A AND B) ≡ NOT(A) OR NOT(B) for any customer.</summary>
    [Property(Arbitrary = [typeof(CustomerArbitraryFactory)], MaxTest = 200)]
    public bool DeMorganFirst_NotAAndB_EqualsNotAOrNotB_ForAnyCustomer(Customer customer)
    {
        var specA = new ActiveCustomerSpecification();
        var specB = new NotDeletedCustomerSpecification();
        var lhs = specA.And(specB).Not().IsSatisfiedBy(customer);
        var rhs = specA.Not().Or(specB.Not()).IsSatisfiedBy(customer);
        return lhs == rhs;
    }

    /// <summary>NOT(A OR B) ≡ NOT(A) AND NOT(B) for any customer.</summary>
    [Property(Arbitrary = [typeof(CustomerArbitraryFactory)], MaxTest = 200)]
    public bool DeMorganSecond_NotAOrB_EqualsNotAAndNotB_ForAnyCustomer(Customer customer)
    {
        var specA = new ActiveCustomerSpecification();
        var specB = new NotDeletedCustomerSpecification();
        var lhs = specA.Or(specB).Not().IsSatisfiedBy(customer);
        var rhs = specA.Not().And(specB.Not()).IsSatisfiedBy(customer);
        return lhs == rhs;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Deterministic regression: Identity laws
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AndIdentity_AAndTrue_EquivalentToA()
    {
        var customers = CreateTestCustomers();
        var spec = new ActiveCustomerSpecification();
        var trueSpec = Spec.True<Customer>();

        var compositeSpec = spec.And(trueSpec);

        foreach (var customer in customers)
        {
            spec.IsSatisfiedBy(customer).Should().Be(
                compositeSpec.IsSatisfiedBy(customer),
                $"A AND TRUE must equal A for customer {customer.Id}");
        }
    }

    [Fact]
    public void OrIdentity_AOrFalse_EquivalentToA()
    {
        var customers = CreateTestCustomers();
        var spec = new ActiveCustomerSpecification();
        var falseSpec = Spec.False<Customer>();

        var compositeSpec = spec.Or(falseSpec);

        foreach (var customer in customers)
        {
            spec.IsSatisfiedBy(customer).Should().Be(
                compositeSpec.IsSatisfiedBy(customer),
                $"A OR FALSE must equal A for customer {customer.Id}");
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Deterministic regression: Annihilator laws
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AndAnnihilator_AAndFalse_AlwaysFalse()
    {
        var customers = CreateTestCustomers();
        var spec = new ActiveCustomerSpecification().And(Spec.False<Customer>());

        foreach (var customer in customers)
        {
            spec.IsSatisfiedBy(customer).Should().BeFalse(
                $"A AND FALSE must always be FALSE for customer {customer.Id}");
        }
    }

    [Fact]
    public void OrAnnihilator_AOrTrue_AlwaysTrue()
    {
        var customers = CreateTestCustomers();
        var spec = new ActiveCustomerSpecification().Or(Spec.True<Customer>());

        foreach (var customer in customers)
        {
            spec.IsSatisfiedBy(customer).Should().BeTrue(
                $"A OR TRUE must always be TRUE for customer {customer.Id}");
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Deterministic regression: Double negation
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void DoubleNegation_NotNotA_EquivalentToA()
    {
        var customers = CreateTestCustomers();
        var spec = new ActiveCustomerSpecification();
        var doubleNegated = spec.Not().Not();

        foreach (var customer in customers)
        {
            spec.IsSatisfiedBy(customer).Should().Be(
                doubleNegated.IsSatisfiedBy(customer),
                $"NOT(NOT(A)) must equal A for customer {customer.Id}");
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Deterministic regression: Commutativity
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AndCommutativity_AAndB_EqualsBAnd()
    {
        var customers = CreateTestCustomers();
        var specA = new ActiveCustomerSpecification();
        var specB = new NotDeletedCustomerSpecification();

        var ab = specA.And(specB);
        var ba = specB.And(specA);

        foreach (var customer in customers)
        {
            ab.IsSatisfiedBy(customer).Should().Be(
                ba.IsSatisfiedBy(customer),
                $"AND must be commutative for customer {customer.Id}");
        }
    }

    [Fact]
    public void OrCommutativity_AOrB_EqualsBAorA()
    {
        var customers = CreateTestCustomers();
        var specA = new ActiveCustomerSpecification();
        var specB = new NotDeletedCustomerSpecification();

        var ab = specA.Or(specB);
        var ba = specB.Or(specA);

        foreach (var customer in customers)
        {
            ab.IsSatisfiedBy(customer).Should().Be(
                ba.IsSatisfiedBy(customer),
                $"OR must be commutative for customer {customer.Id}");
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Deterministic regression: De Morgan's laws
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void DeMorganFirst_NotAAndB_EqualsNotAOrNotB()
    {
        var customers = CreateTestCustomers();
        var specA = new ActiveCustomerSpecification();
        var specB = new NotDeletedCustomerSpecification();

        // NOT(A AND B) ≡ NOT(A) OR NOT(B)
        var leftSide = specA.And(specB).Not();
        var rightSide = specA.Not().Or(specB.Not());

        foreach (var customer in customers)
        {
            leftSide.IsSatisfiedBy(customer).Should().Be(
                rightSide.IsSatisfiedBy(customer),
                $"De Morgan's law violated for customer {customer.Id}");
        }
    }

    [Fact]
    public void DeMorganSecond_NotAOrB_EqualsNotAAndNotB()
    {
        var customers = CreateTestCustomers();
        var specA = new ActiveCustomerSpecification();
        var specB = new NotDeletedCustomerSpecification();

        // NOT(A OR B) ≡ NOT(A) AND NOT(B)
        var leftSide = specA.Or(specB).Not();
        var rightSide = specA.Not().And(specB.Not());

        foreach (var customer in customers)
        {
            leftSide.IsSatisfiedBy(customer).Should().Be(
                rightSide.IsSatisfiedBy(customer),
                $"De Morgan's second law violated for customer {customer.Id}");
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Error handling
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void InvalidCompositionKind_ThrowsException()
    {
        var spec = new CompositeSpecification<Customer>(
            new ActiveCustomerSpecification(),
            new NotDeletedCustomerSpecification(),
            (CompositionKind)99);

        var act = () => spec.IsSatisfiedBy(new Customer());
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Unknown composition kind: 99");
    }

    [Fact]
    public void CompositeSpecification_ConstantFolding_SimplifiesBothLeftAndRightConstants()
    {
        var activeSpec = new ActiveCustomerSpecification();

        // Right operand is constant
        var rightAndTrue = activeSpec.And(Spec.True<Customer>());
        rightAndTrue.ToExpression().Body.NodeType.Should().Be(ExpressionType.MemberAccess,
            "activeSpec AND True should simplify directly to the activeSpec member expression without AndAlso node");

        var rightOrFalse = activeSpec.Or(Spec.False<Customer>());
        rightOrFalse.ToExpression().Body.NodeType.Should().Be(ExpressionType.MemberAccess,
            "activeSpec OR False should simplify directly to the activeSpec member expression without OrElse node");

        // Left operand is constant
        var leftTrueAnd = Spec.True<Customer>().And(activeSpec);
        leftTrueAnd.ToExpression().Body.NodeType.Should().Be(ExpressionType.MemberAccess,
            "True AND activeSpec should simplify directly to the activeSpec member expression without AndAlso node");

        var leftFalseOr = Spec.False<Customer>().Or(activeSpec);
        leftFalseOr.ToExpression().Body.NodeType.Should().Be(ExpressionType.MemberAccess,
            "False OR activeSpec should simplify directly to the activeSpec member expression without OrElse node");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helper
    // ──────────────────────────────────────────────────────────────────────────

    private static Customer[] CreateTestCustomers() =>
    [
        new Customer { Id = 1, IsActive = true, IsDeleted = false, CreditLimit = 5000m },
        new Customer { Id = 2, IsActive = true, IsDeleted = true, CreditLimit = 1000m },
        new Customer { Id = 3, IsActive = false, IsDeleted = false, CreditLimit = 0m },
        new Customer { Id = 4, IsActive = false, IsDeleted = true, CreditLimit = 0m },
        new Customer { Id = 5, IsActive = true, IsDeleted = false, CreditLimit = 50000m }
    ];
}

/// <summary>
/// FsCheck arbitrary factory for <see cref="EricksonLopez.Specification.Tests.Customer"/> instances
/// used in property-based algebra tests.
/// Registered via <c>[Property(Arbitrary = [typeof(CustomerArbitraryFactory)])]</c>.
/// </summary>
/// <remarks>
/// FsCheck discovers static methods returning <see cref="Arbitrary{T}"/> in classes registered
/// via the <c>Arbitrary</c> attribute property.
/// This factory generates Customer instances with all combinations of boolean flags (IsActive, IsDeleted)
/// and bounded credit limits — exercising the full discriminant space of Boolean algebra laws
/// across the 200 random inputs generated per property test.
/// Note: Uses <c>FsCheck.Fluent.Gen</c> (the static C#-friendly factory class) — NOT
/// the generic instance type <c>FsCheck.Gen&lt;T&gt;</c>.
/// </remarks>
public static class CustomerArbitraryFactory
{
    public static Arbitrary<EricksonLopez.Specification.Tests.Customer> Customer()
    {
        // FsCheck 3.x Fluent API: use LINQ SelectMany (query syntax) to compose generators.
        // FsCheck.Fluent.Gen exposes Select/SelectMany extension methods that enable
        // standard C# query comprehension over Gen<T> values.
        // Gen.Choose(lo, hi) → bounded int; Gen.Elements(seq) → picks from finite set.
        var gen =
            from id in Gen.Choose(-1_000_000, 1_000_000)
            from isActive in Gen.Elements(new[] { true, false })
            from isDeleted in Gen.Elements(new[] { true, false })
            from creditRaw in Gen.Choose(0, 100_000)
            select new EricksonLopez.Specification.Tests.Customer
            {
                Id = id,
                IsActive = isActive,
                IsDeleted = isDeleted,
                CreditLimit = (decimal)creditRaw
            };
        return gen.ToArbitrary();
    }
}


