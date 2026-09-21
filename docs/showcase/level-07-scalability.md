# Level 07: Scalability and Performance — Expression Engine

## Overview

Level 7 exposes the internal performance engine that powers the library's AOT-safe, high-throughput expression evaluation.

## Engine Components

### ExpressionHasher
Computes structural hash of expression trees. Two syntactically identical expressions produce the same hash regardless of parameter names.

`csharp
int hash = ExpressionHasher.ComputeHash(expr);
`

### ExpressionSimplifier
Performs constant folding: 	rue && x → x, alse || x → x, NOT(NOT(x)) → x.

`csharp
var simplified = ExpressionSimplifier.Simplify(expr);
// or: var simplified = ExpressionSimplifier.Default.Visit(expr);
`

### ExpressionCompilationCache
Caches compiled IL delegates by expression structural equality. **JIT only** ([RequiresDynamicCode]).

`csharp
var compiled = ExpressionCompilationCache.GetOrCompile(expr);
int cached = ExpressionCompilationCache.CachedCount;
`

### ExpressionInterpreter
AOT-safe evaluator. Walks expression tree at runtime without IL emit.

`csharp
bool result = ExpressionInterpreter.Evaluate(expr, candidate);
`

### ExpressionComposer
Compose expressions without Expression.Invoke (avoids nested parameter issues):

`csharp
var and = ExpressionComposer.And(expr1, expr2);
var or = ExpressionComposer.Or(expr1, expr2);
var not = ExpressionComposer.Not(expr1);
var andAll = ExpressionComposer.AndAll<T>(predicates.AsSpan());
var orAny = ExpressionComposer.OrAny<T>(predicates.AsSpan());
`

### ExpressionEqualityComparer
Structural equality ignoring parameter names:

`csharp
bool equal = ExpressionEqualityComparer.Default.Equals(expr1, expr2);
`

### ExpressionDebugFormatter
Human-readable expression formatting:

`csharp
string formatted = ExpressionDebugFormatter.Format(expr);
`

### SpecificationDiagnostics (OpenTelemetry)

`csharp
// Activity Source for distributed tracing
using var activity = SpecificationDiagnostics.ActivitySource.StartActivity("MyOp");
activity?.SetTag("specification.hash", hash);

// Meters for metrics
// specification.created, specification.evaluated, specification.composed,
// specification.compiled, specification.expression.cache.hits,
// specification.expression.cache.misses, specification.sql.translations
`

## Running Example

See [Level7_Scalability.cs](../../samples/Showcase/Levels/Level7_Scalability.cs).
