# EricksonLopez.Specification -- Product Strategy

> Version: 1.0 -- 2026-08-12
> Input: FEATURES.md + COMPETITIVE_INTELLIGENCE.md
> Stance: This document determines where to invest. It does not assume the library should win.

---

## Section 0 -- Context

### Problem solved
How do you encode a business rule once and use it: in domain validation (in-memory), in SQL (Dapper), in EF Core queries, and in NativeAOT -- without coupling the domain to any ORM?

No currently available .NET library solves this completely:
  - Ardalis: solves EF Core CRUD. Doesn't work with Dapper, doesn't work in AOT, conflates layers.
  - LinqKit: solves expression composition. AOT-incompatible, no abstraction.
  - EF Core native: solves EF Core querying. Dynamic composition incompatible with AOT.

### Target user
PRIMARY: Senior .NET developers and architects who:
  (a) use Dapper alongside or instead of EF Core, OR
  (b) target NativeAOT (.NET 9+, serverless, MAUI, edge), OR
  (c) enforce strict DDD layer separation

NOT THE TARGET: Teams using EF Core exclusively for standard CRUD. Ardalis is better for them today.

### Dimensions that actually drive adoption
1. Correctness -- does it work without bugs (TABLE STAKES)
2. Documentation -- can a developer start in 10 minutes (TABLE STAKES)
3. API ergonomics -- natural, discoverable (TABLE STAKES)
4. Community trust -- downloads, recognizable maintainer (TABLE STAKES)
5. Integration fit -- works with my existing stack (DIFFERENTIATOR)
6. Performance -- hot-path evaluation (DIFFERENTIATOR)
7. AOT compatibility -- growing with .NET 9+ (DIFFERENTIATOR)
8. DDD correctness -- matters to senior architects (DIFFERENTIATOR)

Library currently excels at 5-8 but has critical gaps in 1-4.

---

## Section 1 -- Feature Matrix Audit

### Problems in the current matrix

1. Presence != maturity. "OrderBy" listed as Native, but our LINQ adapter is newer than Ardalis's 10+ years of refinement.

2. MISSING features: XML doc coverage, error messages when translation fails, debug experience, migration guide, CI benchmark regression.

3. OVERSTATED features: AndAll(span), SAT simplification, XOR -- technically interesting, practically irrelevant to 99% of users.

4. UNDERSTATED: Documentation quality, error experience, migration path from Ardalis. These will determine adoption.

5. Comparison injustice: LinqKit is rated on spec features it was never designed to provide.

---

## Section 2 -- Feature Classification

### A. Paridad competitiva (Table Stakes)
  - ISpecification<T> / Specification<T>
  - And / Or / Not
  - IsSatisfiedBy(T)
  - QuerySpec<T> ordering + pagination + projection
  - IReadRepository<T> contract
  - IQueryable<T> adapter
  - CancellationToken in all async APIs
  - XML documentation / IntelliSense
  - Thread safety

### B. Gaps criticos

  GAP-CRIT-01: Trimming annotations ([DynamicallyAccessedMembers]) on ExpressionInterpreter + QuerySpecTranslator
  Without: publish-time ILLink warnings break the AOT claim. Severity: CRITICAL. Blocks primary differentiator.

  GAP-CRIT-02: StartsWith/EndsWith LIKE pattern bug
  Correctness failure. Wrong SQL generated. Severity: CRITICAL. Must fix before any release.

  GAP-CRIT-03: Documentation (no tutorials, no cookbook, no migration guide)
  Largest gap. Technical superiority without docs = zero adoption. Severity: CRITICAL.

  GAP-HIGH-01: IN/.Contains() predicate SQL translation
  Appears in >60% of real queries. Absence blocks collection-based Dapper filters. Severity: HIGH.

  GAP-MED-01: Any(spec) + Count(spec) LINQ extensions (Ardalis has these)
  GAP-MED-02: SingleOrDefaultAsync in IReadRepository<T>

  GAP-LOW-01: SPEC008/009/010 analyzers (SPEC001-007 cover most cases; 008-010 add DI + async)

### C. Fortalezas

  STR-01: NativeAOT compatibility -- no competitor offers this for a spec library
  STR-02: Dapper/SQL-first pipeline -- no competitor exists at the library level
  STR-03: Roslyn analyzers -- no spec library ships analyzers; SPEC003 alone prevents hours of debugging
  STR-04: DDD purity (predicate-only Specification<T>) -- Ardalis is provably architecturally incorrect
  STR-05: Immutable thread-safe QuerySpec<T> as sealed record -- Ardalis spec is mutable
  STR-06: ExpressionComposer (Invoke-free) -- prevents the most common EF Core expression translation error
  STR-07: Structural expression hashing + compiled delegate cache -- cross-instance cache hits

### D. Diferenciadores (Hard-to-replicate)

  DIF-01: AOT-first interpreted evaluation (ExpressionInterpreter)
  To copy: build a full expression interpreter covering all LINQ node types. Months of work.

  DIF-02: Expression-to-SQL pipeline (QuerySpecTranslator + ISqlDialect + QueryModel)
  To copy: build a provider-agnostic SQL AST + expression visitor + dialect. Essentially a mini-ORM.

  DIF-03: SPEC001\u2013SPEC011 Roslyn analyzers
  To copy: 7+ Roslyn analyzers requiring Roslyn expertise + ongoing maintenance.

  DIF-04: DDD architectural purity
  Cannot copy without breaking Ardalis's 18.8M existing users.

### E. Nice-to-have (Do not prioritize)
  - Expression string representation for debug
  - Cursor/keyset pagination (belongs in consumer's repository layer)
  - SPEC005 (ordering without pagination) -- low signal, suppressible
  - Source generator (high cost, limited early value)

### F. Commodities (Everyone has them)
  - And/Or/Not raw composition
  - OrderBy/ThenBy
  - Skip/Take
  - IReadRepository<T> interface

---

## Section 3 -- Strategic Gap Analysis

### MUST BUILD to not fall behind (blocking gaps)

  MUST-01: Trimming annotations
  Without: AOT claim is false. 100% of AOT-targeting users affected.
  Classification: Correctness fix for the primary differentiator.

  MUST-02: LIKE bug fix
  Without: SQL translation is incorrect for string predicates.
  Classification: Correctness fix for the secondary differentiator.

  MUST-03: Documentation + cookbook
  Without: zero adoption regardless of technical quality.
  Classification: Prerequisite for any adoption.

  MUST-04: IN/.Contains() SQL translation
  Without: collection-based predicates cannot be translated. Blocks majority of real Dapper queries.
  Classification: Capability gap that blocks Dapper differentiator.

### SHOULD BUILD for competitive advantage (opportunity gaps)

  SHOULD-01: Public BenchmarkDotNet suite
  Publish honest numbers including 5-20x interpreted overhead. Trust > marketing.

  SHOULD-02: AOT sample project (NativeAOT + Dapper.AOT)
  The only working demo of the primary differentiator.

  SHOULD-03: Any/Count LINQ extensions + SingleOrDefaultAsync
  Reduces Ardalis migration friction at minimal implementation cost.

  SHOULD-04: MSSQL dialect
  Doubles addressable market for SQL-first differentiator.

  SHOULD-05: SPEC008-010 analyzers
  Reinforces the only-lib-with-analyzers position.


---

## Section 4 -- Competitive Advantage Map

### Real (structurally defensible, months to copy)
  1. AOT-first interpreted evaluation: full expression interpreter for all LINQ node types
  2. Expression-to-SQL pipeline: provider-agnostic AST + visitor + dialect = mini-ORM build
  3. Roslyn analyzer suite: growing catalog, Roslyn expertise required
  4. DDD-correct architecture: cannot change Ardalis without breaking 18.8M users

### Real but copiable in weeks
  5. Immutable QuerySpec<T> record: Ardalis could add this in a minor version
  6. Spec.True<T>() / Spec.False<T>(): simple to copy
  7. ExpressionSimplifier (constant folding): 2-4 weeks with ExpressionVisitor

### NOT real advantages (parity, not differentiation)
  8. And/Or/Not: every library has this
  9. IReadRepository<T>: standard pattern
  10. Ordering/Pagination in QuerySpec: Ardalis has equivalent

### Developer experience advantages (underappreciated)
  1. Testing: spec.IsSatisfiedBy(entity) -- no DbContext, no mock, no IQueryable. Pure unit test.
  2. SPEC003: catches Expression.Invoke silently failing in EF Core. Prevents hours of debugging.
  3. SPEC001: prevents unsealed specification hierarchy bugs common in DDD teams.
  4. Immutable QuerySpec: cannot accidentally mutate a shared specification across threads.

---

## Section 5 -- Opportunity Prioritization

Scoring: User Impact (1-5), Market Demand (1-5), Competitive Pressure (1-5),
         Strategic Differentiation (1-5), Adoption Potential (1-5),
         Implementation Effort (1=hard, 5=easy), Technical Risk (1=risky, 5=safe)

Priority Score = (Impact + Demand + Pressure + Diff + Adoption) x Effort x Risk

| Opportunity | Impact | Demand | Pressure | Diff | Adoption | Effort | Risk | Score |
|---|---|---|---|---|---|---|---|---|
| Trimming annotations | 5 | 5 | 5 | 5 | 5 | 4 | 4 | 400 |
| Fix LIKE bug | 5 | 4 | 4 | 2 | 5 | 5 | 5 | 1000 |
| Documentation + cookbook | 5 | 5 | 4 | 3 | 5 | 3 | 5 | 660 |
| AOT sample project | 4 | 4 | 4 | 5 | 5 | 4 | 5 | 880 |
| IN/.Contains() SQL translation | 4 | 4 | 3 | 4 | 4 | 4 | 4 | 608 |
| BenchmarkDotNet public suite | 3 | 3 | 3 | 5 | 4 | 3 | 5 | 540 |
| Any(spec) + Count(spec) LINQ | 3 | 3 | 4 | 1 | 3 | 5 | 5 | 700 |
| SingleOrDefaultAsync | 2 | 3 | 3 | 1 | 2 | 5 | 5 | 275 |
| MSSQL dialect | 3 | 3 | 2 | 2 | 3 | 3 | 3 | 243 |
| SPEC008-SPEC010 analyzers | 3 | 2 | 1 | 4 | 3 | 3 | 4 | 312 |
| Expression debug representation | 2 | 2 | 1 | 2 | 2 | 4 | 5 | 360 |
| PostgreSQL ILIKE | 2 | 2 | 1 | 2 | 2 | 4 | 5 | 360 |
| Source generator (full) | 2 | 1 | 1 | 3 | 2 | 2 | 3 | 54 |

Note: LIKE bug scores highest because correctness at zero adoption stage is existential.
Trimming annotations scores lower only due to implementation complexity but is MUST-DO for the AOT story.

---

## Section 6 -- Opportunity Map

| Opportunity | Type | Impact | Competitive Pressure | Differentiation | Effort | Risk | Status |
|---|---|---|---|---|---|---|---|
| Fix LIKE bug | Critical Bug | 5 | 5 | N/A | Low | Low | MUST DO |
| Trimming annotations | Critical Gap | 5 | 5 | 5 | Medium | Low | MUST DO |
| Documentation + cookbook | Critical Gap | 5 | 4 | 3 | High | Low | MUST DO |
| AOT sample project | Strategic | 4 | 4 | 5 | Medium | Low | MUST DO |
| IN/.Contains() SQL | High Gap | 4 | 3 | 4 | Medium | Med | MUST DO |
| BenchmarkDotNet public | Strategic | 3 | 3 | 5 | Medium | Low | SHOULD DO |
| Any/Count LINQ | Med Gap | 3 | 4 | 1 | Low | Low | SHOULD DO |
| SingleOrDefaultAsync | Med Gap | 2 | 3 | 1 | Low | Low | SHOULD DO |
| MSSQL dialect | Extension | 3 | 3 | 2 | Medium | Med | SHOULD DO |
| SPEC008-010 analyzers | Strength | 3 | 1 | 4 | Medium | Low | SHOULD DO |
| Expression debug | Developer XP | 2 | 1 | 2 | Low | Low | COULD DO |
| PostgreSQL ILIKE | Extension | 2 | 1 | 2 | Low | Low | COULD DO |
| Source generator (full) | Strategic | 2 | 1 | 3 | High | High | COULD DO |
| Cursor/keyset pagination | Feature | 2 | 1 | 1 | High | Med | DON'T DO |
| XOR composition | Feature | 1 | 1 | 0 | Medium | Low | DON'T DO |
| Write repository | Scope creep | 1 | 4 | 0 | Medium | Low | DON'T DO |
| Include/ThenInclude in core | Scope creep | 2 | 4 | 0 | High | High | DON'T DO |
| Dynamic string ordering | Anti-pattern | 1 | 2 | 0 | Medium | High | DON'T DO |
| FluentValidation integration | Scope creep | 1 | 1 | 0 | Medium | Low | DON'T DO |
| SAT-based simplification | Academic | 1 | 0 | 1 | Very High | Very High | DON'T DO |

---

## Section 7 -- Product Roadmap

### NOW (0-3 months) -- Correctness and Credibility
These are not features. They are the minimum bar for a credible public release.

---

NOW-01: Fix StartsWith/EndsWith LIKE pattern bug
Problem: QuerySpecTranslator generates wrong SQL for LIKE predicates.
Initiative: Fix TranslateStringMethod -> correct pattern for arg%, %arg, %arg%.
Impact: All string LIKE predicates in SQL translation become correct.
Risk: Low. Isolated fix.
Metric: All LIKE-related translator tests pass. No regression.

---

NOW-02: Trimming annotations on ExpressionInterpreter + QuerySpecTranslator
Problem: PropertyInfo.GetValue and FieldInfo.GetValue lack [DynamicallyAccessedMembers].
Publishing with PublishAot=true emits ILLink warnings -> AOT claim is false.
Initiative: Add [DynamicallyAccessedMembers(PublicProperties | NonPublicFields)] where needed.
Add [RequiresUnreferencedCode] on paths where full safety cannot be guaranteed.
Impact: Zero ILLink warnings on dotnet publish -p:PublishAot=true.
Risk: Low. Annotation-only change. No behavioral change.
Metric: Zero warnings on core + sql + postgres + dapper packages in AOT publish.

---

NOW-03: IN/.Contains() predicate SQL translation
Problem: Enumerable.Contains(x.Property) is in >60% of real queries. Untranslatable today.
Initiative: Add Contains pattern in QuerySpecTranslator. Emit ANY() or IN (,) via ISqlDialect.
Impact: Collection-based predicates translate to SQL correctly.
Risk: Medium. Requires dialect + parameter changes.
Metric: c => ids.Contains(c.Id) produces WHERE id = ANY(@p1) in PostgreSQL dialect.

---

NOW-04: Documentation + cookbook
Problem: Zero tutorials. No real-world examples. Developer cannot start without reading source code.
Initiative:
  (a) README.md: 10-minute quickstart (domain spec, query spec, Dapper, EF Core paths)
  (b) docs/cookbook.md: 10 real scenarios (active customers, premium tier, audit trail, DDD aggregate)
  (c) docs/migration-from-ardalis.md: Ardalis concept -> EricksonLopez equivalent mapping
  (d) Audit all public APIs for XML doc comment completeness
Risk: Low. But requires sustained writing effort.
Metric: A .NET developer unfamiliar with the library can build a Dapper + spec query in <20 min from README alone.

---

NOW-05: AOT sample project (NativeAOT + Dapper.AOT)
Problem: No working demonstration that AOT claim is true.
Initiative: samples/NativeAotDapper/ -- .NET 9 NativeAOT app:
  (a) Specification<T> for domain validation
  (b) QuerySpec<T> for query building
  (c) QuerySpecTranslator + PostgreSqlDialect for SQL generation
  (d) Dapper.AOT for execution
  (e) Publishes with PublishAot=true, zero warnings, executes correctly
Dependencies: NOW-02 (trimming annotations must be complete).
Risk: Medium. NativeAOT + Dapper.AOT setup has integration complexity.
Metric: CI build passes with PublishAot=true. App executes SQL correctly against real database.

---

### NEXT (3-6 months) -- Adoption and Completeness

NEXT-01: Public BenchmarkDotNet suite
Problem: We claim performance advantages without published benchmarks.
Initiative: Implement Scenarios A-H (FEATURES.md). Compare EricksonLopez (interpreted + compiled), Ardalis, LinqKit, raw delegate.
IMPORTANT: Publish the 5-20x interpreted overhead number with context. Hiding it creates distrust.
Risk: Medium. Results may reveal issues requiring fixes.
Metric: Public BenchmarkDotNet results in docs/benchmarks.md with documented methodology.

---

NEXT-02: Any(spec) + Count(spec) LINQ extensions + SingleOrDefaultAsync
Problem: Ardalis users expect these. Absence creates friction for migrators.
Initiative: Add to QuerySpecLinqExtensions and IReadRepository<T>.
Risk: Low.
Metric: APIs exist and pass tests. No breaking change.

---

NEXT-03: MSSQL dialect
Problem: PostgreSQL-only SQL translation excludes most enterprise teams.
MSSQL is the dominant enterprise database.
Initiative: MsSqlDialect : ISqlDialect with TSQL specifics (TOP, square bracket quoting, parameterization differences).
Risk: Medium. MSSQL dialect has quirks.
Metric: Correct TSQL output for all supported predicates. Integration tests against SQL Server.

---

NEXT-04: SPEC008-SPEC010 analyzers
Initiative: SPEC008 (DI in spec), SPEC009 (constructor DI dependencies), SPEC010 (async lambda in expression).
Risk: Low. Roslyn analyzer pattern established.
Metric: All three fire correctly on intentionally bad code. No false positives on reference projects.

---

### LATER (6-12 months) -- Strategic Positioning

LATER-01: SQLite dialect
Value: Enables integration testing without external server. SQLite is the standard test database.
Risk: Low.

LATER-02: Expression structural equality
Value: Enables SQL plan caching with perfect cache keys (different lambda parameter names = equal).
Risk: Medium. Full equality for all expression node types is complex.

LATER-03: SQL query plan cache
Value: Cache SQL string by expression hash. First call: translate + render. Subsequent: hash lookup.
Dependencies: LATER-02 (expression equality as cache key).
Risk: Medium. Cache invalidation + parameter substitution complexity.

LATER-04: Ardalis migration tooling
Value: Roslyn codefix or analyzer that suggests EricksonLopez equivalents when Ardalis patterns detected.
Risk: High. Codegen migration is complex. May need to be a documentation guide first.

---

## Section 8 -- What NOT to Build

### NEVER: IRepository<T> write repository
Reason: Not a Specification concern. Inflates scope. Destroys minimalist philosophy.
Anti-pattern: Ardalis chose to include writes. That is why Ardalis is now a God library.
Alternative: Consumers implement write repositories. Document the pattern clearly.

### NEVER: Include / ThenInclude in core
Reason: EF Core-specific. Pollutes the Dapper API. Creates IIncludableQueryable dependency.
Anti-pattern: Ardalis has this and it locks them into EF Core-centric design.
Alternative: Consumers implement includes in their EF Core infrastructure layer. Document this.

### NEVER: Dynamic string ordering (OrderBy("Name"))
Reason: Runtime errors, reflection, security risk in SQL contexts, contradicts type-safety pillar.
Alternative: Strongly typed OrderBy<TKey> already exists.

### NEVER: SAT-based predicate simplification
Reason: Academic. Zero practical payoff. ExpressionSimplifier covers the 99% case.

### NEVER: XOR composition
Reason: No DDD use case documented. Zero demand in any competitor library.

### NEVER: FluentValidation integration in core
Reason: FluentValidation is a complete separate framework. Different API contract. Dependency bloat.

---

## Section 9 -- Competitive Positioning

### Positioning Statement (full)

For senior .NET developers and architects practicing DDD or Clean Architecture with Dapper or NativeAOT,
who need to encode business rules as reusable predicates that work in memory, against any database, and in AOT environments,
EricksonLopez.Specification is the Specification Pattern library that delivers
AOT-safe in-memory evaluation, SQL translation for Dapper, and strict architectural separation between domain predicates and query intent,
unlike Ardalis.Specification (which couples layers and lacks Dapper and AOT support) and LinqKit (which is an expression utility rather than a specification library and is AOT-incompatible),
because it is the only implementation designed from the start with Native AOT, Dapper, and DDD as first-class architectural constraints.

---

### 1-sentence version
The only .NET specification library that works in NativeAOT, translates to Dapper SQL, and keeps your domain layer clean.

---

### 30-second version
Most .NET specification libraries were designed for EF Core. They break in NativeAOT, provide nothing for Dapper, and encourage you to mix ordering and pagination into your domain specifications.

EricksonLopez.Specification is different. Your domain specifications are pure predicates -- evaluatable in memory, translatable to SQL, composable without any ORM dependency. It ships with Roslyn analyzers that catch architectural mistakes at compile time. It works in NativeAOT by design.

It is the specification library for developers who care about correctness, not just convenience.

---

### 3 Value Propositions

VP-01 (for AOT developers):
The only specification library that compiles to NativeAOT without workarounds.
ExpressionInterpreter evaluates specifications in memory without Expression.Compile.
Your domain rules work the same in development, production, and serverless.

VP-02 (for Dapper developers):
The missing link between specification pattern and Dapper.
Write your filter once as a typed expression. Get back parameterized SQL automatically.
No raw string concatenation. No SQL injection risk. Works with any SQL database.

VP-03 (for DDD architects):
The specification library that actually respects DDD.
Specification<T> is a pure predicate -- no ordering, no pagination, no ORM concerns.
QuerySpec<T> is the query descriptor. Roslyn analyzers enforce the boundary at compile time.

---

### 3 Homepage Messages

MSG-01: Write your business rule once. Use it in memory, in EF Core, in Dapper, in NativeAOT.
MSG-02: The only specification library that translates to Dapper SQL and compiles to NativeAOT.
MSG-03: A DDD-correct, AOT-first specification library with built-in Roslyn analyzers.

---

### 3 Why Us vs. Competitor arguments

vs. Ardalis:
Ardalis is excellent for EF Core CRUD teams. But if you use Dapper or target NativeAOT, it does not help you.
EricksonLopez was designed for exactly those scenarios -- and it keeps your domain specifications architecturally clean.

vs. LinqKit:
LinqKit is a predicate builder, not a specification library. No domain abstraction, no in-memory evaluation, no SQL translation, no NativeAOT compatibility. It silently breaks without .AsExpandable() in EF Core.
EricksonLopez gives you all of that in a single coherent library.

vs. rolling your own:
Yes, you can compose Expression<Func<T,bool>> manually. But then you need caching, AOT-safe evaluation, a query descriptor, SQL translation, and analyzers to prevent misuse.
That is what EricksonLopez provides -- start with the primitives you would build yourself, finished and battle-tested.

---

## Section 10 -- Differentiation Strategy

### 3 selected pillars (do not dilute beyond these)

Pillar 1: AOT-First Engineering
  Why it matters: NativeAOT adoption grows with every .NET release. Serverless, MAUI, edge.
  Evidence: EF Core AOT is experimental + static-only. No competitor spec library supports AOT. Structural advantage.
  How to reinforce: Complete trimming annotations. Publish AOT sample. Write: "The only specification library for NativeAOT".
  Watch: Ardalis GitHub for AOT issues (would require complete redesign to address).

Pillar 2: SQL-First / Dapper Integration
  Why it matters: Dapper 500M+ downloads. Large percentage of .NET shops use Dapper. They have NO spec library today.
  Evidence: Confirmed market gap -- community resorts to manual SQL strings. No library-level solution exists.
  How to reinforce: Fix LIKE bug. Add IN/.Contains(). Add MSSQL dialect. Publish Dapper + EricksonLopez cookbook.
  Watch: Dapper.AOT GitHub for expression-to-SQL roadmap. If Marc Gravell adds it natively, adapt.

Pillar 3: Architectural Correctness (DDD + Analyzers)
  Why it matters: Senior architects care. Ardalis cannot change its architecture without breaking 18.8M users.
  Evidence: Documented with code examples that Ardalis mixes domain + query + ORM in one class.
  How to reinforce: Publish ADRs. Grow analyzer catalog to SPEC010+. Engage DDD/Clean Architecture community.
  Watch: If Ardalis ships analyzers, our analyzer differentiator weakens -- but AOT + Dapper remain.

---

## Section 11 -- Metrics

### Adoption (primary)
  - NuGet downloads/week (target: 100/week at 6mo, 500/week at 12mo)
  - Package install ratio (Core : Dapper : Linq) -- tells us which segment is adopting
  - GitHub stars (target: 100 at 6mo, 500 at 12mo)

### Quality (leading indicator of adoption)
  - Open P0 bugs (must be 0 for correctness claim)
  - ILLink warnings on AOT publish (must be 0 for AOT claim)
  - Test coverage % (maintain >90%)

### Community
  - GitHub issues opened (engagement proxy)
  - External PRs (community health)
  - Mentions on Reddit r/dotnet, StackOverflow, .NET blog posts

### Competitive
  - "Migrated from Ardalis" issues/PRs
  - StackOverflow citations as Ardalis alternative
  - Dapper community mentions

### DO NOT TRACK
  - Total feature count (features != value)
  - SPEC diagnostic count (more diagnostics is not better)
  - Lines of code (no proxy for quality)

---

## Section 12 -- Scenario Analysis

### Strategy A -- Catch-up (close gaps with Ardalis)

Action: Add Include, write repository, deep EF Core integration. Become a superset of Ardalis.

Risks:
  - Requires breaking the architectural philosophy that is the primary differentiator.
  - Adding Include/write repository destroys DDD purity.
  - Ardalis wins on ecosystem trust regardless (18.8M downloads).
  - Result: "Ardalis but with AOT" -- insufficient reason to switch.

RECOMMENDATION: REJECT. Destroys the unique value proposition.

---

### Strategy B -- Balanced (fix critical gaps + invest in differentiation)

Action: Fix bugs, trimming, IN translation, documentation. Simultaneously invest in AOT sample and Dapper story.

Risks:
  - Documentation effort is large and deprioritized by technical maintainers.
  - Balanced strategy can produce mediocre results at both ends.

Better outcome than A: preserves differentiation while achieving minimum viability.

---

### Strategy C -- Differentiation-First (accept some gaps, own the niche)

Action: Focus purely on AOT + Dapper. Do not add write repository or Include. Build the best AOT-first spec library.

Risks:
  - Smaller addressable market.
  - Documentation is still required (cannot skip regardless of strategy).
  - If critical bugs are not fixed, differentiation claim is still false.

Best outcome: Reference implementation for AOT-first Dapper specification. Smaller but loyal user base.

---

### Recommendation: Modified Strategy B (Differentiation-anchored Balanced)

Fix NOW list (correctness + AOT credibility). These are prerequisites, not strategy.
Then pursue Strategy C's posture: differentiation-first for the next 12 months.
Do NOT add Include, write repository, or dynamic ordering.
Balanced element: add LINQ parity (Any, Count, SingleOrDefault) at low cost to reduce migration friction.

Strategy: Fix the floor. Own the ceiling.

---

## Section 13 -- Executive Decision

### 1. Our biggest competitive problem is:
Documentation. Not features. Not bugs (bugs must be fixed, but docs determines adoption).
Without documentation, every other investment produces zero return.

### 2. Our greatest strength is:
Being the only library that solves three problems simultaneously:
  (a) AOT-safe in-memory evaluation
  (b) Expression-to-SQL translation for Dapper
  (c) DDD-correct architecture with compiler-enforced guardrails
None can be copied quickly. The combination is unique.

### 3. Our greatest opportunity is:
The Dapper + NativeAOT intersection. As AOT adoption grows, Dapper teams will discover they have no spec library.
EricksonLopez can own this niche before any competitor invests in it.

### 4. The 3 features to prioritize are:
  1. Documentation + cookbook (prerequisite for any adoption)
  2. AOT sample project (proves the primary claim, enables word-of-mouth)
  3. IN/.Contains() SQL translation (enables real-world Dapper queries; LIKE fix is a prerequisite)

### 5. The 3 things we must NOT build are:
  1. IRepository<T> write repository (scope creep, destroys minimalist philosophy)
  2. Include/ThenInclude in core (ORM coupling, destroys provider independence)
  3. Dynamic string ordering (runtime errors, reflection, type-safety contradiction)

### 6. Our primary differentiator should be:
"The only specification library built for AOT + Dapper + DDD -- all three simultaneously."
Not AOT-capable. Not Dapper-compatible. All three, architecturally correct.
No competitor can claim this today and cannot take it quickly.

### 7. The recommended roadmap is:
NOW: Fix LIKE bug. Add trimming annotations. Add IN translation. Write documentation. Build AOT sample.
NEXT: Publish benchmarks. Add LINQ parity. Add MSSQL dialect. Add SPEC008-010 analyzers.
LATER: SQLite dialect. Expression equality. SQL plan cache. Migration tooling from Ardalis.

### 8. The recommended competitive strategy is:
Fix the floor (correctness + trimming). Own the ceiling (AOT + Dapper).
Do not compete with Ardalis for EF Core CRUD teams.
Own the underserved segment: .NET developers using NativeAOT and/or Dapper who need a spec library.

### 9. The metric to watch most is:
NuGet downloads/week segmented by package.
If EricksonLopez.Specification.Dapper grows alongside core: Dapper story is working.
If only core grows but Dapper/SQL do not: reaching wrong users.
This ratio tells us whether the differentiated segment is adopting.

### 10. The strategic decision to make now is:
Publish nothing until documentation exists and critical bugs are fixed.
The worst outcome: early adopters find the LIKE bug, discover trimming warnings, post negative reviews.
In developer communities, negative first impressions are nearly impossible to reverse.

Sequence: Fix bugs -> Add trimming annotations -> Write documentation -> Build AOT sample -> Release.

The library is technically strong. Make it publicly credible before making it publicly available.
Do not release early. Do not release incomplete.

