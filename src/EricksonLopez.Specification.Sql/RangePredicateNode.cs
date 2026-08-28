// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.Specification.Sql;

/// <summary>
/// Represents a range inclusion predicate on a column.
/// </summary>
/// <param name="ColumnName">The SQL column name.</param>
/// <param name="LowerParameterName">The lower bound parameter name.</param>
/// <param name="UpperParameterName">The upper bound parameter name.</param>
/// <param name="Inclusive">A value indicating whether bounds are inclusive.</param>
public sealed record RangePredicateNode(
    string ColumnName,
    string LowerParameterName,
    string UpperParameterName,
    bool Inclusive = true) : SqlPredicateNode;
