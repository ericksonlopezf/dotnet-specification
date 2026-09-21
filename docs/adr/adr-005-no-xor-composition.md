# adr-005: No XOR Composition or Additional Boolean Operators

## Status
Accepted

## Date
2026-08-12

**Status**: Accepted  
**Date**: 2026-08-12  
**Deciders**: EricksonLopez.Specification architecture audit  
**Category**: API Surface / Scope

---

## Context

It was evaluated whether the library should expose additional boolean operators beyond `And()`, `Or()`, and `Not()`:

- **XOR** (`^`): `A XOR B` — exactly one of the two is true
- **NAND**: `NOT (A AND B)`
- **NOR**: `NOT (A OR B)`
- **XNOR**: `NOT (A XOR B)` — logical equivalence
- **Implication**: `A → B` ≡ `!A || B`

## Decision

**REJECTED. The composition API is limited to `And()`, `Or()`, and `Not()`.**

## Rationale

1. **Zero documented demand in DDD.** Across all DDD resources (Evans, Vernon, Millett) there is not a single example of an XOR specification. There are no issues, PRs, or discussions in Ardalis, LinqKit, or any other competitor requesting XOR.

2. **XOR does not map naturally to business rules.** Business rules are expressed as "the customer must be active AND premium", "the customer is active OR in a grace period", "the customer is NOT deleted". "The customer is exactly active or exactly premium but not both" is a requirement designed from the data model, not from the domain.

3. **XOR is trivially composed when needed.** `A XOR B` = `(A || B) && !(A && B)`. A developer can build it with the existing operators.

4. **API surface inflation.** Each additional operator is one more method in the fluent API, more documentation, more tests, more maintenance surface. The marginal return is negative.

5. **SQL translation problem.** XOR does not exist as an operator in standard T-SQL. Its translation requires expansion: `(A OR B) AND NOT (A AND B)`. Additional complexity in `QuerySpecTranslator` for a nonexistent use case.

## Consequences

- **Positive**: Minimal and focused API surface. Lower cognitive load for new users.
- **Negative**: None documented — no real use case exists that requires it.

## Alternative for Those Who Need XOR

```csharp
// XOR implemented with existing operators
var xor = specA.Or(specB).And(specA.And(specB).Not());

// Note: if you find yourself needing XOR,
// the business rule is likely incorrectly modeled. Review the domain.
```
