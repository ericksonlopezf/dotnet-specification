// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.Specification.Sql;

/// <summary>
/// Represents an IN predicate comparing a column against a collection of values.
/// </summary>
/// <param name="ColumnName">The SQL column name.</param>
/// <param name="ParameterName">The SQL parameter name.</param>
/// <param name="Negated">A value indicating whether the IN condition is negated.</param>
public sealed record InPredicateNode(
    string ColumnName,
    string ParameterName,
    bool Negated = false) : SqlPredicateNode;
