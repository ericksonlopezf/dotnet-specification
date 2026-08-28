// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace EricksonLopez.Specification;

/// <summary>
/// Provides human-readable string formatting for predicate expression trees.
/// </summary>
/// <remarks>
/// <para>
/// <strong>AOT safety:</strong> This formatter uses <see cref="ExpressionVisitor"/> and
/// does NOT reflect on candidate values — only on expression metadata (member names, constants).
/// It is fully AOT-safe.
/// </para>
/// <para>
/// <strong>Usage:</strong>
/// <code>
/// var spec = new ActivePremiumCustomer(500m);
/// string debug = spec.ToDebugString();
/// // Output: "(IsActive == true) AND (TotalPurchases >= 500)"
/// </code>
/// </para>
/// </remarks>
public sealed class ExpressionDebugFormatter : ExpressionVisitor
{
    /// <summary>Gets the singleton instance of the formatter.</summary>
    public static readonly ExpressionDebugFormatter Default = new();

    private ExpressionDebugFormatter() { }

    /// <summary>
    /// Formats the specified expression tree as a human-readable string.
    /// </summary>
    /// <typeparam name="T">The predicate input type.</typeparam>
    /// <param name="expression">The expression tree to format.</param>
    /// <returns>A formatted string representation of the expression.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="expression"/> is <see langword="null"/></exception>
    public static string Format<T>(Expression<Func<T, bool>> expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        var builder = new FormatVisitor();
        builder.Visit(expression.Body);
        return builder.ToString();
    }

    private sealed class FormatVisitor : ExpressionVisitor
    {
        private readonly StringBuilder _sb = new();

        public override string ToString() => _sb.ToString();

        protected override Expression VisitBinary(BinaryExpression node)
        {
            // Stryker disable once Statement : Internal visitor node is validated by ExpressionVisitor base
            ArgumentNullException.ThrowIfNull(node);

            if (node.NodeType == ExpressionType.AndAlso)
            {
                _sb.Append('(');
                Visit(node.Left);
                _sb.Append(") AND (");
                Visit(node.Right);
                _sb.Append(')');
                return node;
            }

            if (node.NodeType == ExpressionType.OrElse)
            {
                _sb.Append('(');
                Visit(node.Left);
                _sb.Append(") OR (");
                Visit(node.Right);
                _sb.Append(')');
                return node;
            }

            if (node.NodeType == ExpressionType.Coalesce)
            {
                _sb.Append('(');
                Visit(node.Left);
                _sb.Append(" ?? ");
                Visit(node.Right);
                _sb.Append(')');
                return node;
            }

            Visit(node.Left);
            _sb.Append(' ');
            _sb.Append(FormatOperator(node.NodeType));
            _sb.Append(' ');
            Visit(node.Right);
            return node;
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            // Stryker disable once Statement : Internal visitor node is validated by ExpressionVisitor base
            ArgumentNullException.ThrowIfNull(node);

            if (node.NodeType == ExpressionType.Not)
            {
                _sb.Append("NOT(");
                Visit(node.Operand);
                _sb.Append(')');
                return node;
            }

            Visit(node.Operand);
            return node;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            // Stryker disable once Statement : Internal visitor node is validated by ExpressionVisitor base
            ArgumentNullException.ThrowIfNull(node);

            // c.PropertyName → just show PropertyName
            if (node.Expression is ParameterExpression)
            {
                _sb.Append(node.Member.Name);
                return node;
            }

            // Closure captured value — extract via reflection (safe: no candidate reflection)
            if (node.Expression is ConstantExpression constExpr)
            {
                var val = node.Member is System.Reflection.FieldInfo fi
                    ? fi.GetValue(constExpr.Value)
                    : ((System.Reflection.PropertyInfo)node.Member).GetValue(constExpr.Value);
                FormatValue(val);
                return node;
            }

            _sb.Append(node.Member.Name);
            return node;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            // Stryker disable once Statement : Internal visitor node is validated by ExpressionVisitor base
            ArgumentNullException.ThrowIfNull(node);
            FormatValue(node.Value);
            return node;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            // Stryker disable once Statement : Internal visitor node is validated by ExpressionVisitor base
            ArgumentNullException.ThrowIfNull(node);

            // String instance methods: c.Name.Contains("X") → Name CONTAINS("X")
            if (node.Object?.Type == typeof(string))
            {
                Visit(node.Object);
                _sb.Append(' ');
                _sb.Append(node.Method.Name switch
                {
                    nameof(string.Contains) => "CONTAINS",
                    nameof(string.StartsWith) => "STARTS_WITH",
                    nameof(string.EndsWith) => "ENDS_WITH",
                    _ => node.Method.Name
                });
                _sb.Append('(');
                if (node.Arguments.Count > 0) Visit(node.Arguments[0]);
                _sb.Append(')');
                return node;
            }

            // collection.Contains(c.Id) → Id IN (...)
            if (node.Method.Name == "Contains")
            {
                var columnExpr = (node.Object is null && node.Arguments.Count == 2)
                    ? node.Arguments[1]
                    : (node.Object is not null && node.Arguments.Count == 1)
                        ? node.Arguments[0]
                        : null;

                if (columnExpr is not null)
                {
                    Visit(columnExpr);
                    _sb.Append(" IN (...)");
                    return node;
                }
            }

            // c.Age.Between(18, 65) → Age BETWEEN (18, 65)
            if (node.Method.Name == "Between")
            {
                if (node.Object is not null && node.Arguments.Count == 2)
                {
                    Visit(node.Object);
                    _sb.Append(" BETWEEN (");
                    Visit(node.Arguments[0]);
                    _sb.Append(", ");
                    Visit(node.Arguments[1]);
                    _sb.Append(')');
                    return node;
                }

                if (node.Object is null && node.Arguments.Count == 3)
                {
                    Visit(node.Arguments[0]);
                    _sb.Append(" BETWEEN (");
                    Visit(node.Arguments[1]);
                    _sb.Append(", ");
                    Visit(node.Arguments[2]);
                    _sb.Append(')');
                    return node;
                }
            }

            // Generic: MethodName(args)
            _sb.Append(node.Method.Name);
            _sb.Append('(');
            for (var i = 0; i < node.Arguments.Count; i++)
            {
                if (i > 0) _sb.Append(", ");
                Visit(node.Arguments[i]);
            }
            _sb.Append(')');
            return node;
        }

        protected override Expression VisitConditional(ConditionalExpression node)
        {
            // Stryker disable once Statement : Internal visitor node is validated by ExpressionVisitor base
            ArgumentNullException.ThrowIfNull(node);
            _sb.Append('(');
            Visit(node.Test);
            _sb.Append(" ? ");
            Visit(node.IfTrue);
            _sb.Append(" : ");
            Visit(node.IfFalse);
            _sb.Append(')');
            return node;
        }

        protected override Expression VisitTypeBinary(TypeBinaryExpression node)
        {
            // Stryker disable once Statement : Internal visitor node is validated by ExpressionVisitor base
            ArgumentNullException.ThrowIfNull(node);
            Visit(node.Expression);
            _sb.Append(" IS ");
            _sb.Append(node.TypeOperand.Name);
            return node;
        }

        private void FormatValue(object? value)
        {
            _sb.Append(value switch
            {
                null => "null",
                string s => $"\"{s}\"",
                bool b => b ? "true" : "false",
                _ => value.ToString() ?? "null"
            });
        }

        private static string FormatOperator(ExpressionType type) => type switch
        {
            ExpressionType.Equal => "==",
            ExpressionType.NotEqual => "!=",
            ExpressionType.GreaterThan => ">",
            ExpressionType.GreaterThanOrEqual => ">=",
            ExpressionType.LessThan => "<",
            ExpressionType.LessThanOrEqual => "<=",
            _ => type.ToString()
        };
    }
}


