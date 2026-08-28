// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Specification.Sql;

/// <summary>
/// Defines a contract for resolving C# property names to SQL column names.
/// </summary>
public interface IColumnNameResolver
{
    /// <summary>
    /// Resolves a C# property name to a SQL column name.
    /// </summary>
    /// <param name="propertyName">The C# property name (e.g., "IsActive").</param>
    /// <returns>The SQL column name (e.g., "is_active").</returns>
    string Resolve(string propertyName);
}
