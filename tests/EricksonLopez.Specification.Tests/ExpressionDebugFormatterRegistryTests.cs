// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq.Expressions;
using AwesomeAssertions;
using EricksonLopez.Specification;
using Xunit;

namespace EricksonLopez.Specification.Tests;

public sealed class ExpressionDebugFormatterRegistryTests
{
    [Fact]
    public void Format_WhenExpressionNull_ThrowsArgumentNullException()
    {
        var act = () => ExpressionDebugFormatterRegistry.Format(null!);
        act.Should().ThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("expression");
    }

    [Fact]
    public void Formatter_SetCustomFormatter_UsesCustomFormatter()
    {
        var original = ExpressionDebugFormatterRegistry.Formatter;
        try
        {
            ExpressionDebugFormatterRegistry.Formatter = expr => "custom_output";
            Expression<Func<int, bool>> expr = x => x > 5;

            var result = ExpressionDebugFormatterRegistry.Format(expr);
            result.Should().Be("custom_output");
        }
        finally
        {
            ExpressionDebugFormatterRegistry.Formatter = original;
        }
    }

    [Fact]
    public void Formatter_SetNull_FallsBackToDefaultToString()
    {
        var original = ExpressionDebugFormatterRegistry.Formatter;
        try
        {
            ExpressionDebugFormatterRegistry.Formatter = null!;
            Expression<Func<int, bool>> expr = x => x > 5;

            var result = ExpressionDebugFormatterRegistry.Format(expr);
            result.Should().Be(expr.ToString());
        }
        finally
        {
            ExpressionDebugFormatterRegistry.Formatter = original;
        }
    }
}
