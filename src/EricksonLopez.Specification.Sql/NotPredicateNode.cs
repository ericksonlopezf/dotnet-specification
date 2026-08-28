// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.Specification.Sql;

/// <summary>
/// Represents a logical NOT negation of a SQL predicate.
/// </summary>
/// <param name="Inner">The inner predicate to negate.</param>
public sealed record NotPredicateNode(SqlPredicateNode Inner) : SqlPredicateNode;
