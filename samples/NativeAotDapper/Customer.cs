// Copyright © Erickson Lopez. MIT License.
namespace NativeAotDapper;

internal sealed class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string Tier { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
}
