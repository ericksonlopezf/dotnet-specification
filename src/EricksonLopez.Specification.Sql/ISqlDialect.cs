// Copyright © Erickson Lopez. MIT License.
namespace EricksonLopez.Specification.Sql;

/// <summary>
/// Defines the contract for SQL dialect implementations that render query models into dialect-specific SQL.
/// </summary>
public interface ISqlDialect
{
    /// <summary>Gets the human-readable name of this dialect.</summary>
    string DialectName { get; }

    /// <summary>
    /// Renders the specified <see cref="QueryModel"/> into dialect-specific SQL.
    /// </summary>
    /// <param name="model">The query model to render.</param>
    /// <returns>A rendered SQL query with parameters.</returns>
    SqlQuery Render(QueryModel model);

    /// <summary>
    /// Quotes an identifier according to dialect rules.
    /// </summary>
    /// <param name="identifier">The identifier to quote.</param>
    /// <returns>The quoted identifier string.</returns>
    string QuoteIdentifier(string identifier);

    /// <summary>Gets the parameter prefix used by this dialect.</summary>
    string ParameterPrefix { get; }
}

