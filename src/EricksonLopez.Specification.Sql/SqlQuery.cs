// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;

namespace EricksonLopez.Specification.Sql;

/// <summary>
/// Represents the rendered output of a SQL translation, containing a parameterized SQL string and named parameters.
/// </summary>
/// <remarks>
/// All generated SQL is fully parameterized. Literal values are never interpolated
/// directly into the SQL string to prevent SQL injection.
/// </remarks>
public sealed record SqlQuery
{
    /// <summary>Gets the parameterized SQL string.</summary>
    public required string Sql { get; init; }

    /// <summary>
    /// Gets the parameters as a dictionary suitable for use with Dapper, Npgsql, or other drivers.
    /// Keys are parameter names (without the @ or $ prefix depending on dialect).
    /// </summary>
    public required IReadOnlyDictionary<string, object?> Parameters { get; init; }

    /// <summary>
    /// Gets an empty SQL query.
    /// </summary>
    public static readonly SqlQuery Empty = new()
    {
        // Stryker disable once String : default empty
        Sql = string.Empty,
        Parameters = new Dictionary<string, object?>()
    };
}


