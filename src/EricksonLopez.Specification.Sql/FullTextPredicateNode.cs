// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.Specification.Sql;

/// <summary>
/// Represents a full-text search predicate on a column.
/// </summary>
/// <param name="ColumnName">The SQL column name.</param>
/// <param name="ParameterName">The search phrase parameter name.</param>
/// <param name="Language">The text search configuration language.</param>
public sealed record FullTextPredicateNode(
    string ColumnName,
    string ParameterName,
    string Language = "english") : SqlPredicateNode;
