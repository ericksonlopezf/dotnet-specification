// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace EricksonLopez.Specification.Showcase.Levels;

/// <summary>
/// Defines a showcase level that demonstrates a specific feature or concept of the framework.
/// </summary>
public interface ILevel
{
    /// <summary>Gets the display name of the level.</summary>
    string Name { get; }

    /// <summary>Gets a brief description of what the level demonstrates.</summary>
    string Description { get; }

    /// <summary>
    /// Executes the level's demonstration logic.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ExecuteAsync();
}



