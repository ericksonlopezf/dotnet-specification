// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.Specification.Sql;

/// <summary>
/// Represents a BETWEEN range predicate on a column.
/// </summary>
/// <param name="ColumnName">The SQL column name.</param>
/// <param name="LowerParameterName">The lower bound parameter name.</param>
/// <param name="UpperParameterName">The upper bound parameter name.</param>
/// <param name="Negated">A value indicating whether the BETWEEN condition is negated.</param>
public sealed record BetweenPredicateNode(
    string ColumnName,
    string LowerParameterName,
    string UpperParameterName,
    bool Negated = false) : SqlPredicateNode;
