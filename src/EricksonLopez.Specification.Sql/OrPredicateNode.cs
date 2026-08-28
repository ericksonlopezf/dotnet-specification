// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.Specification.Sql;

/// <summary>
/// Represents a logical OR composition of two SQL predicates.
/// </summary>
/// <param name="Left">The left operand predicate.</param>
/// <param name="Right">The right operand predicate.</param>
public sealed record OrPredicateNode(SqlPredicateNode Left, SqlPredicateNode Right) : SqlPredicateNode;
