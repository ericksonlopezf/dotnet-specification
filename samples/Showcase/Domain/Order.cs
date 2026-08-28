// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Specification.Showcase.Domain;

/// <summary>
/// Represents a purchase order placed by a customer.
/// </summary>
public class Order
{
    /// <summary>Gets or sets the unique identifier for the order.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Gets or sets the unique identifier of the customer who placed the order.</summary>
    public Guid CustomerId { get; set; }

    /// <summary>Gets or sets the total monetary amount of the order.</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Gets or sets the current processing status of the order.</summary>
    public OrderStatus Status { get; set; }

    /// <summary>Gets or sets the date and time when the order was placed.</summary>
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
}
