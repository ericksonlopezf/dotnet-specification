// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Linq.Expressions;
using AwesomeAssertions;
using Xunit;

namespace EricksonLopez.Specification.Tests;

/// <summary>
/// Tests for internal <see cref="ParameterReplacer"/> expression visitor.
/// </summary>
public sealed class ParameterReplacerTests
{
    [Fact]
    public void Replace_WhenSourceEqualsTarget_ReturnsOriginalExpression()
    {
        var param = Expression.Parameter(typeof(Customer), "c");
        var expr = Expression.Equal(param, Expression.Constant(null));

        var result = ParameterReplacer.Replace(expr, param, param);

        ReferenceEquals(expr, result).Should().BeTrue();
    }

    [Fact]
    public void Replace_VisitParameter_DifferentParameter_ReturnsOriginal()
    {
        var source = Expression.Parameter(typeof(Customer), "c");
        var target = Expression.Parameter(typeof(Customer), "t");
        var different = Expression.Parameter(typeof(Customer), "other");

        var replaced = ParameterReplacer.Replace(different, source, target);

        replaced.Should().BeSameAs(different);
    }

    [Fact]
    public void Replace_ReplacesMatchingParameter()
    {
        var source = Expression.Parameter(typeof(Customer), "c");
        var target = Expression.Parameter(typeof(Customer), "t");
        var prop = Expression.Property(source, nameof(Customer.IsActive));

        var replaced = ParameterReplacer.Replace(prop, source, target);

        var memberExpr = replaced.Should().BeAssignableTo<MemberExpression>().Subject;
        memberExpr.Expression.Should().BeSameAs(target);
    }
}


