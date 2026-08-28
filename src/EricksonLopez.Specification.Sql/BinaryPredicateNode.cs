// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.Specification.Sql;

/// <summary>
/// Represents a binary comparison predicate comparing a column against a parameterized value.
/// </summary>
/// <param name="ColumnName">The SQL column name.</param>
/// <param name="Operator">The binary comparison operator.</param>
/// <param name="ParameterName">The SQL parameter name.</param>
public sealed record BinaryPredicateNode(
    string ColumnName,
    SqlBinaryOperator Operator,
    string ParameterName) : SqlPredicateNode;
