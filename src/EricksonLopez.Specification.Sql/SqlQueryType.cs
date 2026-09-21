// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.Specification.Sql;

/// <summary>Specifies the type of SQL query to generate.</summary>
public enum SqlQueryType
{
    /// <summary>Specifies a standard SELECT query returning rows.</summary>
    Select,
    /// <summary>Specifies a COUNT(*) query returning the row count.</summary>
    Count,
    /// <summary>Specifies an EXISTS query returning whether matching rows exist.</summary>
    Exists
}
