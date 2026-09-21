// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;

namespace EricksonLopez.Specification.Showcase.Domain;

/// <summary>
/// Represents a customer entity within the showcase domain.
/// </summary>
public class Customer
{
    /// <summary>Gets or sets the unique identifier for the customer.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Gets or sets the full name of the customer.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the email address of the customer.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the customer is active.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Gets or sets the date and time when the customer was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the total number of purchases made by the customer.</summary>
    public int TotalPurchases { get; set; }

    /// <summary>Gets or sets the credit limit granted to the customer.</summary>
    public decimal CreditLimit { get; set; }

    /// <summary>Gets or sets the optional promotional discount rate.</summary>
    public decimal? DiscountRate { get; set; }

    /// <summary>Gets or sets the collection of orders associated with the customer.</summary>
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
