// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using EricksonLopez.Specification.Sql;

namespace EricksonLopez.Specification.Sql.Tests;

/// <summary>
/// Domain model used across SQL dialect test suites.
/// </summary>
public class TestCustomer
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public decimal CreditLimit { get; init; }
    public string Region { get; init; } = string.Empty;
    public string? Email { get; init; }
}

/// <summary>
/// Abstract base class for dialect rendering unit tests.
/// Provides preconfigured translators and helper assertion methods.
/// </summary>
/// <typeparam name="TDialect">The concrete SQL dialect type.</typeparam>
public abstract class DialectTestBase<TDialect> where TDialect : ISqlDialect
{
    /// <summary>Gets the dialect under test.</summary>
    protected abstract TDialect Dialect { get; }

    /// <summary>Gets the default translator using verbatim column names.</summary>
    protected QuerySpecTranslator<TestCustomer> VerbatimTranslator { get; } =
        new("Customers", VerbatimColumnNameResolver.Default);

    /// <summary>Gets the default translator using snake_case column names.</summary>
    protected QuerySpecTranslator<TestCustomer> SnakeCaseTranslator { get; } =
        new("customers", SnakeCaseColumnNameResolver.Default);

    /// <summary>Translates a specification and renders SQL using the tested dialect.</summary>
    protected SqlQuery Render(QuerySpec<TestCustomer> spec, QuerySpecTranslator<TestCustomer>? translator = null)
    {
        var trans = translator ?? VerbatimTranslator;
        var model = trans.Translate(spec);
        return Dialect.Render(model);
    }
}
