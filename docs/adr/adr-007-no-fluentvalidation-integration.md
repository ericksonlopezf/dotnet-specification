# adr-007: No Native FluentValidation Integration

**Status**: Accepted  
**Date**: 2026-08-12  
**Deciders**: EricksonLopez.Specification architecture audit  
**Category**: Scope / Dependencies

---

## Context

It was evaluated whether the library should include native integration with FluentValidation, given that both libraries work with predicates over entities. A potential pattern would be:

```csharp
// Hypothetical — NOT implemented
public class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator(IReadRepository<Customer> repo)
    {
        RuleFor(cmd => cmd.Email)
            .MustSatisfySpec(new ValidEmailSpec())  // hypothetical
            .WithMessage("Invalid email");
    }
}
```

## Decision

**REJECTED. EricksonLopez.Specification will not include native FluentValidation integration in any of its packages.**

## Rationale

1. **FluentValidation is a complete framework with its own object model.** `AbstractValidator<T>`, `IRuleBuilder<T,TProperty>`, `ValidationContext<T>`, and the entire validation pipeline are not directly "specification-compatible". Any integration would require non-trivial adapters.

2. **Unnecessary dependency.** Adding `FluentValidation` as a transitive dependency in `EricksonLopez.Specification.Core` would impose it on all consumers who don't need it.

3. **Real use cases are trivial without integration.** The Specification Pattern already provides `IsSatisfiedBy(T)`. In a FluentValidation context it is used directly:

```csharp
RuleFor(cmd => cmd.Customer)
    .Must(c => new ActiveCustomer().IsSatisfiedBy(c))
    .WithMessage("Customer must be active");
```

No adapter is needed. It is 1-line glue code that the consumer writes in their application layer.

4. **Different semantics.** FluentValidation validates *input commands* (request objects) with user-facing error messages. The Specification Pattern encapsulates *domain rules* evaluated against entities. Merging the two in a unified framework blurs both boundaries.

5. **Zero-dependency strategy for core.** The `EricksonLopez.Specification` (core) package must have zero NuGet dependencies beyond the framework. Every dependency is a compatibility-breaking vector.

## Consequences

- **Positive**: Core has no dependencies. Consumers only add dependencies they actually use.
- **Negative**: Users who want to integrate with FluentValidation write the glue code themselves (1 line).
- **Mitigation**: Document the integration pattern as an example in the cookbook if real demand emerges.

## Possible Future

If documented demand exists (>20 GitHub issues requesting the integration), a separate optional package could be considered: `EricksonLopez.Specification.FluentValidation` that is not a transitive dependency of core. This decision does not block that future possibility.
