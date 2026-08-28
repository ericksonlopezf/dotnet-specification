// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.Specification.Sql;

/// <summary>Specifies the binary comparison operators used in SQL predicates.</summary>
public enum SqlBinaryOperator
{
    /// <summary>Compares two values for equality (<c>=</c>).</summary>
    Equal,
    /// <summary>Compares two values for inequality (<c>&lt;&gt;</c>).</summary>
    NotEqual,
    /// <summary>Tests that the left operand is strictly greater than the right (<c>&gt;</c>).</summary>
    GreaterThan,
    /// <summary>Tests that the left operand is greater than or equal to the right (<c>&gt;=</c>).</summary>
    GreaterThanOrEqual,
    /// <summary>Tests that the left operand is strictly less than the right (<c>&lt;</c>).</summary>
    LessThan,
    /// <summary>Tests that the left operand is less than or equal to the right (<c>&lt;=</c>).</summary>
    LessThanOrEqual,
    /// <summary>LIKE with <c>%arg%</c> wrapping (string.Contains).</summary>
    Like,
    /// <summary>LIKE with <c>arg%</c> prefix pattern (string.StartsWith).</summary>
    LikeStartsWith,
    /// <summary>LIKE with <c>%arg</c> suffix pattern (string.EndsWith).</summary>
    LikeEndsWith,
    /// <summary>Negated pattern match — column does NOT match the pattern (NOT LIKE).</summary>
    NotLike,
    /// <summary>Tests that the column value is SQL NULL (IS NULL).</summary>
    IsNull,
    /// <summary>Tests that the column value is not SQL NULL (IS NOT NULL).</summary>
    IsNotNull
}
