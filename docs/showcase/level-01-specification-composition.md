# Level 01: Specification Composition & Business Rules

## 1. Defining Reusable Specifications
Specifications encapsulate domain validation and filtering rules in isolated classes:

```csharp
using EricksonLopez.Specification;

public sealed class ActiveUserSpec : Specification<User>
{
    public ActiveUserSpec()
    {
        Query.Where(u => u.IsActive && u.EmailConfirmed);
    }
}

public sealed class PremiumCustomerSpec : Specification<User>
{
    public PremiumCustomerSpec(decimal minimumSpend)
    {
        Query.Where(u => u.TotalSpend >= minimumSpend);
    }
}
```

---

## 2. Logical Operators & Chaining
Combine specifications using standard logical operators (`&`, `|`, `!`):

```csharp
var targetAudience = new ActiveUserSpec() & new PremiumCustomerSpec(1000m);

// Evaluate directly on domain aggregate
bool isEligible = targetAudience.IsSatisfiedBy(currentUser);
```
