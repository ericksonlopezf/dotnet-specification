// Copyright © Erickson Lopez. MIT License.
namespace EricksonLopez.Specification;

/// <summary>Defines the logical operation used to compose two specifications.</summary>
internal enum CompositionKind
{
    /// <summary>Logical AND — both specifications must be satisfied.</summary>
    And,

    /// <summary>Logical OR — at least one specification must be satisfied.</summary>
    Or
}

