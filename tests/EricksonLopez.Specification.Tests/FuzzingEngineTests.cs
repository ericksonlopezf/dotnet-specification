// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Linq.Expressions;
using AwesomeAssertions;
using EricksonLopez.Specification;
using EricksonLopez.Specification.Sql;
using Xunit;

namespace EricksonLopez.Specification.Tests;

public sealed class FuzzEntity
{
    public int? NullableInt { get; init; }
    public string? Text { get; init; }
    public decimal? Amount { get; init; }
    public bool Flag { get; init; }
}

public sealed class FuzzingEngineTests
{
    [Fact]
    public void Fuzz_ExtremeDepth_CompositionStackLimit()
    {
        // Compose a tree of depth 500
        var spec = Spec.True<FuzzEntity>();
        for (int i = 0; i < 500; i++)
        {
            spec = spec.And(Spec.For<FuzzEntity>(e => e.Flag == (i % 2 == 0)));
        }

        var candidate = new FuzzEntity { Flag = true };

        // Test if expression evaluates without crashing
        var act = () => spec.IsSatisfiedBy(candidate);
        act.Should().NotThrow<NullReferenceException>();
    }

    [Fact]
    public void Fuzz_NullPropertyPredicates_DoesNotThrowUnexpectedExceptions()
    {
        var entityWithNulls = new FuzzEntity
        {
            NullableInt = null,
            Text = null,
            Amount = null,
            Flag = false
        };

        var spec1 = Spec.For<FuzzEntity>(e => e.Text == null);
        var spec2 = Spec.For<FuzzEntity>(e => e.NullableInt == null);
        var spec3 = Spec.For<FuzzEntity>(e => e.Amount > 0m);

        spec1.IsSatisfiedBy(entityWithNulls).Should().BeTrue();
        spec2.IsSatisfiedBy(entityWithNulls).Should().BeTrue();
        spec3.IsSatisfiedBy(entityWithNulls).Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("'; DROP TABLE Users; --")]
    [InlineData("\0\uFFFF\uD800")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    public void Fuzz_SqlTranslator_PathologicalStringsInSearch(string fuzzInput)
    {
        var translator = new QuerySpecTranslator<FuzzEntity>("fuzz_entities");

        if (string.IsNullOrWhiteSpace(fuzzInput))
        {
            // TagWith throws on whitespace
            var act = () => QuerySpec<FuzzEntity>.Empty.TagWith(fuzzInput);
            act.Should().Throw<ArgumentException>();
        }
        else
        {
            var querySpec = QuerySpec<FuzzEntity>.Empty.Search(fuzzInput, e => e.Text);
            var plan = translator.Translate(querySpec);
            plan.Should().NotBeNull();
            plan.Parameters.Should().ContainSingle(p => (string)p.Value! == $"%{fuzzInput}%");
        }
    }
}
