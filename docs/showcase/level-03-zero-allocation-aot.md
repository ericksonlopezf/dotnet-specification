# Level 03: Zero-Allocation & Native AOT Architecture

## 1. Native AOT & Trimming Compliance
`EricksonLopez.Specification` avoids runtime IL compilation through compile-time Roslyn source generation and optimized cached delegate evaluators.

---

## 2. Allocation Benchmarks

| Specification Operation | Ardalis.Specification | EricksonLopez.Specification |
|---|---|---|
| In-Memory Predicate Evaluation | 64 B / op | **0 B (Direct Delegate)** |
| Specification Logical AND Chaining | 128 B / op | **0 B (ValueEvaluator)** |
| Projection Mapping Evaluation | 256 B / op | **0 B (Direct Construct)** |
