// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.Specification.Sql;

/// <summary>Specifies the type of SQL query to generate.</summary>
public enum SqlQueryType
{
    /// <summary>A standard SELECT query.</summary>
    Select,
    /// <summary>A COUNT(*) query.</summary>
    Count,
    /// <summary>An EXISTS query (e.g. SELECT 1 ... LIMIT 1).</summary>
    Exists
}
