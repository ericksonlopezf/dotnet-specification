# Level 08: Customization — Implementing Public Interfaces

## Overview

Level 8 demonstrates how to extend the library by implementing its public interfaces without modifying library source code.

## IColumnNameResolver

Maps C# property names to SQL column names.

### Built-in Resolvers

| Resolver | IsActive → | TotalPurchases → |
|---|---|---|
| SnakeCaseColumnNameResolver.Default | is_active | 	otal_purchases |
| VerbatimColumnNameResolver.Default | IsActive | TotalPurchases |

### Custom Resolver Example

`csharp
public sealed class LegacyDbColumnNameResolver : IColumnNameResolver
{
    public string Resolve(string propertyName)
        => "TBL_COL_" + propertyName.ToUpperInvariant();
}
// IsActive → TBL_COL_ISACTIVE
`

### Explicit Mapping Resolver

`csharp
var resolver = new ExplicitMappingColumnNameResolver(new Dictionary<string, string>
{
    { "IsActive", "ACTIVE" },
    { "TotalPurchases", "TOTAL_PURCHASES" }
});
`

## ISqlDialect

Full custom SQL generation engine:

`csharp
public sealed class LegacyReportingDialect : ISqlDialect
{
    public string DialectName => "LegacyReporting";
    public string ParameterPrefix => ":";
    public string QuoteIdentifier(string identifier) => identifier.ToUpperInvariant();
    public SqlQuery Render(QueryModel model) { /* build SQL */ }
}
`

## Usage with QuerySpecTranslator

`csharp
var translator = new QuerySpecTranslator<Customer>("CUSTOMERS", new LegacyDbColumnNameResolver());
var model = translator.Translate(spec);
var sql = new LegacyReportingDialect().Render(model);
`

## Running Example

See [Level8_Customization.cs](../../samples/Showcase/Levels/Level8_Customization.cs).
