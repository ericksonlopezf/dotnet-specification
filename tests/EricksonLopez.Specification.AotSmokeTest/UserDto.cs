// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.Specification.AotSmokeTest;

/// <summary>
/// DTO representing a user for Native AOT smoke testing.
/// </summary>
public sealed class UserDto
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int Age { get; set; }
}
