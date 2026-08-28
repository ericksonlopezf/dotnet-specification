// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.Specification.Sql;

/// <summary>
/// Represents a column ordering instruction for a SQL ORDER BY clause.
/// </summary>
/// <param name="ColumnName">The SQL column name.</param>
/// <param name="Direction">The ordering direction.</param>
public sealed record SqlOrderNode(string ColumnName, OrderDirection Direction);
