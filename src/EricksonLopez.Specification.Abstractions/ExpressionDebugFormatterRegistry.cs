// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq.Expressions;

namespace EricksonLopez.Specification;

/// <summary>
/// Provides a registry for expression debug formatting across architectural layers.
/// </summary>
public static class ExpressionDebugFormatterRegistry
{
    private static Func<Expression, string> _formatter = expr => expr.ToString();

    /// <summary>
    /// Gets or sets the debug formatter delegate.
    /// </summary>
    public static Func<Expression, string> Formatter
    {
        get => _formatter;
        set => _formatter = value ?? (expr => expr.ToString());
    }

    /// <summary>
    /// Formats an expression using the registered formatter.
    /// </summary>
    /// <param name="expression">The expression to format.</param>
    /// <returns>A formatted string representation of the expression.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="expression"/> is <see langword="null"/></exception>
    public static string Format(Expression expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        return _formatter(expression);
    }
}
