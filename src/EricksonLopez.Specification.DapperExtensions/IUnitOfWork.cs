// Copyright © Erickson Lopez. MIT License.
using System.Data;

namespace EricksonLopez.DapperExtensions.UnitOfWork;

/// <summary>
/// Defines the contract for an active unit of work that manages database transactions.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Gets the database transaction associated with this unit of work.
    /// </summary>
    IDbTransaction Transaction { get; }
}
