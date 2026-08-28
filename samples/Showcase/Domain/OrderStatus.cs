// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.Specification.Showcase.Domain;

/// <summary>
/// Specifies the processing status of a purchase order.
/// </summary>
public enum OrderStatus
{
    /// <summary>The order has been received but not yet processed.</summary>
    Pending,

    /// <summary>The order is currently being processed.</summary>
    Processing,

    /// <summary>The order has been shipped to the customer.</summary>
    Shipped,

    /// <summary>The order has been delivered to the customer.</summary>
    Delivered,

    /// <summary>The order was cancelled and will not be fulfilled.</summary>
    Cancelled
}
