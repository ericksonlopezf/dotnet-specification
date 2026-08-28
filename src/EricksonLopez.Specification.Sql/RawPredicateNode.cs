// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.Specification.Sql;

/// <summary>
/// A raw SQL predicate for unsupported expression patterns. Internal to dialect implementations.
/// </summary>
/// <param name="Sql">The raw SQL string.</param>
internal sealed record RawPredicateNode(string Sql) : SqlPredicateNode;
