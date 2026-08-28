# Level 02: Evaluators & Multi-Provider Architecture

## 1. Provider Isolation
`EricksonLopez.Specification` separates domain specification definitions from infrastructure-specific evaluators:
- `EricksonLopez.Specification.EntityFrameworkCore`
- `EricksonLopez.Specification.Dapper`
- `EricksonLopez.Specification.Linq`

```csharp
// Evaluate against EF Core DbSet
var query = dbContext.Users.WithSpecification(new ActiveUserSpec());

// Evaluate against In-Memory ReadOnlySpan/List
var filteredList = userList.Where(new ActiveUserSpec().ToPredicate());
```

---

## 2. Dynamic Sorting & Pagination Support
Specifications natively support strongly typed ordering, pagination (`Skip`/`Take`), and projection (`Select`):

```csharp
public sealed class PaginatedOrdersSpec : Specification<Order, OrderDto>
{
    public PaginatedOrdersSpec(int pageIndex, int pageSize)
    {
        Query.Where(o => o.Status == OrderStatus.Completed)
             .OrderByDescending(o => o.PlacedAt)
             .Skip((pageIndex - 1) * pageSize)
             .Take(pageSize)
             .Select(o => new OrderDto(o.Id, o.TotalAmount, o.PlacedAt));
    }
}
```
