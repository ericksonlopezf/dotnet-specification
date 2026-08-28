// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.Specification;

/// <summary>
/// Specifies the seek direction for cursor-based (keyset) pagination.
/// </summary>
public enum CursorDirection
{
    /// <summary>Seeks records occurring after the cursor value.</summary>
    After = 0,

    /// <summary>Seeks records occurring before the cursor value.</summary>
    Before = 1
}
