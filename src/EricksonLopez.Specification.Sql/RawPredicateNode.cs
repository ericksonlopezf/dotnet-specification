// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.Specification.Sql;

/// <summary>
/// Represents a raw SQL predicate for unsupported expression patterns.
/// </summary>
/// <param name="Sql">The raw SQL string.</param>
internal sealed record RawPredicateNode(string Sql) : SqlPredicateNode;
