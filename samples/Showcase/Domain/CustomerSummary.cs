// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Specification.Showcase.Domain;

/// <summary>
/// A lightweight projection of <see cref="Customer"/> for read-heavy scenarios.
/// Used to demonstrate <see cref="EricksonLopez.Specification.QuerySpec{T,TResult}"/>.
/// </summary>
public sealed record CustomerSummary(Guid Id, string Name, int TotalPurchases);
