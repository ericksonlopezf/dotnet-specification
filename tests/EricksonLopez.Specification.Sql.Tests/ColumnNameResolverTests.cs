// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Specification.Sql;
using Xunit;

namespace EricksonLopez.Specification.Sql.Tests;

public sealed class ColumnNameResolverTests
{
    [Fact]
    public void SnakeCase_Resolve_NullOrWhiteSpace_Throws()
    {
        var act1 = () => SnakeCaseColumnNameResolver.Default.Resolve(null!);
        act1.Should().Throw<ArgumentException>();

        var act2 = () => SnakeCaseColumnNameResolver.Default.Resolve(" ");
        act2.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("IsActive", "is_active")]
    [InlineData("CreditLimit", "credit_limit")]
    [InlineData("CustomerId", "customer_id")]
    [InlineData("Id", "id")]
    [InlineData("A", "a")]
    [InlineData("TotalPurchasesUSD", "total_purchases_u_s_d")]
    [InlineData("already_snake", "already_snake")]
    public void SnakeCase_Resolve_ConvertsCorrectly(string input, string expected)
    {
        var result = SnakeCaseColumnNameResolver.Default.Resolve(input);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("PascalCase")]
    [InlineData("camelCase")]
    [InlineData("snake_case")]
    [InlineData("Column123")]
    public void Verbatim_Resolve_ReturnsOriginal(string input)
    {
        var name = VerbatimColumnNameResolver.Default.Resolve(input);
        name.Should().Be(input);
    }

    [Fact]
    public void Verbatim_Resolve_NullOrWhiteSpace_Throws()
    {
        var act1 = () => VerbatimColumnNameResolver.Default.Resolve(null!);
        act1.Should().Throw<ArgumentException>();

        var act2 = () => VerbatimColumnNameResolver.Default.Resolve(" ");
        act2.Should().Throw<ArgumentException>();
    }
}




