// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Immutable;

namespace EricksonLopez.Specification.Sql;

/// <summary>
/// Represents a provider-agnostic intermediate representation of a SQL query.
/// Produced from a <see cref="QuerySpec{T}"/> and consumed by an <see cref="ISqlDialect"/>
/// to generate dialect-specific SQL.
/// </summary>
public sealed record QueryModel
{
    /// <summary>Gets the name of the table or view being queried.</summary>
    public required string TableName { get; init; }

    /// <summary>Gets the optional table alias to use in the query.</summary>
    public string? TableAlias { get; init; }

    /// <summary>Gets the filter predicates in the SQL AST form.</summary>
    public ImmutableArray<SqlPredicateNode> Filters { get; init; } = [];

    /// <summary>Gets the ordering clauses.</summary>
    public ImmutableArray<SqlOrderNode> Orders { get; init; } = [];

    /// <summary>Gets the column projections. Empty means SELECT *.</summary>
    public ImmutableArray<string> Projections { get; init; } = [];

    /// <summary>Gets the number of rows to skip (OFFSET).</summary>
    public int? Skip { get; init; }

    /// <summary>Gets the maximum number of rows to return (LIMIT / TOP).</summary>
    public int? Take { get; init; }

    /// <summary>Gets a value indicating whether to apply DISTINCT.</summary>
    public bool IsDistinct { get; init; }

    /// <summary>Gets the type of query (Select, Count, Exists).</summary>
    public SqlQueryType QueryType { get; init; } = SqlQueryType.Select;

    /// <summary>Gets the collected SQL parameters for this query.</summary>
    public ImmutableArray<SqlParameter> Parameters { get; init; } = [];
}



