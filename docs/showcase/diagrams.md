# Architecture Diagrams — EricksonLopez.Specification
<!-- Phase 7: Diagrams based exclusively on the discovered public API -->

---

## 1. General Package Architecture

```mermaid
graph TD
    A["EricksonLopez.Specification.Abstractions\nISpecification, IExpressionSpecification,\nIReadRepository, QuerySpec, ExpressionDebugFormatterRegistry"]
    B["EricksonLopez.Specification\n(Core Engine)\nSpecification, Spec, ExpressionComposer,\nExpressionHasher, ExpressionSimplifier,\nExpressionInterpreter, ExpressionCompilationCache,\nSpecificationDiagnostics"]
    C["EricksonLopez.Specification.Linq\nQuerySpecLinqExtensions:\nApply, Any, Count"]
    D["EricksonLopez.Specification.Sql\nQuerySpecTranslator, ISqlDialect,\nQueryModel, QueryPlanCache,\nIColumnNameResolver"]
    E1["PostgreSqlDialect"]
    E2["MsSqlDialect"]
    E3["SqliteDialect"]
    E4["MySqlDialect"]
    E5["MariaDbDialect"]
    E6["OracleDialect"]
    F["EricksonLopez.Specification.Dapper\nQuerySpecDapperExtensions:\nQueryAsync, QueryFirstOrDefaultAsync,\nCountAsync, AnyAsync"]
    G["EricksonLopez.Specification.EntityFrameworkCore\nEfSpecificationEvaluator, ISpecificationEvaluator,\nEfReadRepository, QuerySpecEfCoreExtensions,\nSpecificationEntityFrameworkServiceCollectionExtensions"]
    H["EricksonLopez.Specification.MongoDB\nMongoSpecificationEvaluator,\nMongoSpecificationExtensions"]
    I["EricksonLopez.Specification.Result\nReadRepositoryResultExtensions"]

    A --> B
    B --> C
    B --> D
    D --> E1
    D --> E2
    D --> E3
    D --> E4
    D --> E5
    D --> E6
    E1 --> F
    E2 --> F
    E3 --> F
    A --> G
    A --> H
    A --> I
```

---

## 2. Main Workflow: Definition → Evaluation

```mermaid
sequenceDiagram
    participant Domain as Domain Layer
    participant Spec as Specification&lt;T&gt;
    participant Engine as Expression Engine
    participant Repo as IReadRepository&lt;T&gt;
    participant Infra as Infrastructure

    Domain->>Spec: new ActiveCustomerSpecification()
    Note over Spec: Diagnostics.SpecificationsCreated++
    Spec->>Engine: BuildExpression() lazy cached
    Domain->>Spec: IsSatisfiedBy(customer)
    Spec->>Engine: ExpressionInterpreter.Evaluate(expr, candidate)
    Note over Engine: AOT-safe, no IL emit
    Engine-->>Domain: bool

    Domain->>Repo: ListAsync(QuerySpec&lt;T&gt;)
    Repo->>Infra: EF Core / Dapper / MongoDB
    Infra-->>Domain: IReadOnlyList&lt;T&gt;
```

---

## 3. QuerySpec Construction Pipeline

```mermaid
flowchart LR
    A["QuerySpec&lt;T&gt;.Empty"]
    B[".Where(predicate)"]
    C[".And(Specification&lt;T&gt;)"]
    D[".OrderBy / .OrderByDescending"]
    E[".ThenBy / .ThenByDescending"]
    F[".Page(page, pageSize)"]
    G[".Take(n) / .Skip(n)"]
    H[".Distinct()"]
    I[".TagWith(tag)"]
    J[".WithCursor / .SeekAfter / .SeekBefore"]
    K[".Select() -> QuerySpec&lt;T,TResult&gt;"]

    A --> B --> C --> D --> E --> F --> G --> H --> I --> J --> K
```

---

## 4. SQL Translation Pipeline

```mermaid
sequenceDiagram
    participant App as Application
    participant TS as "QuerySpecTranslator&lt;T&gt;"
    participant PC as QueryPlanCache
    participant AST as QueryModel
    participant Dialect as ISqlDialect
    participant DB as IDbConnection

    App->>TS: Translate(querySpec)
    TS->>PC: TryGetPlan(criteria, tableName)
    alt Cache Hit
        PC-->>TS: QueryModel cached
    else Cache Miss
        TS->>TS: Walk expression tree
        TS->>TS: Build SqlPredicateNode AST
        TS->>AST: QueryModel with Filters/Orders/Skip/Take
        TS->>PC: SetPlan(criteria, tableName, model)
    end
    App->>Dialect: Render(model)
    Dialect-->>App: SqlQuery { Sql, Parameters }
    App->>DB: QueryAsync&lt;T&gt;(sql, parameters)
    DB-->>App: IEnumerable&lt;T&gt;
```

---

## 5. SQL Predicate AST

```mermaid
classDiagram
    class SqlPredicateNode {
        abstract
    }
    class BinaryPredicateNode {
        string Column
        SqlBinaryOperator Operator
        object Value
    }
    class InPredicateNode {
        string Column
        IEnumerable Values
    }
    class BetweenPredicateNode {
        string Column
        object Lower
        object Upper
    }
    class FullTextPredicateNode {
        string Column
        string SearchTerm
    }
    class RangePredicateNode {
        string Column
        object Lower
        object Upper
    }
    class AndPredicateNode {
        SqlPredicateNode Left
        SqlPredicateNode Right
    }
    class OrPredicateNode {
        SqlPredicateNode Left
        SqlPredicateNode Right
    }
    class NotPredicateNode {
        SqlPredicateNode Inner
    }

    SqlPredicateNode <|-- BinaryPredicateNode
    SqlPredicateNode <|-- InPredicateNode
    SqlPredicateNode <|-- BetweenPredicateNode
    SqlPredicateNode <|-- FullTextPredicateNode
    SqlPredicateNode <|-- RangePredicateNode
    SqlPredicateNode <|-- AndPredicateNode
    SqlPredicateNode <|-- OrPredicateNode
    SqlPredicateNode <|-- NotPredicateNode
```

---

## 6. Specification Composition Hierarchy

```mermaid
classDiagram
    class ISpecification~T~ {
        interface
        bool IsSatisfiedBy(T)
    }
    class IExpressionSpecification~T~ {
        interface
        Expression ToExpression()
        string ToDebugString()
    }
    class Specification~T~ {
        abstract
        BuildExpression()
        IsSatisfiedBy(T)
        ToExpression()
        ToCompiledPredicate()
        ToQuerySpec()
        And(other)
        Or(other)
        Not()
        operator and
        operator or
        operator not
    }
    class LambdaSpecification~T~
    class CompositeSpecification~T~
    class NegatedSpecification~T~

    ISpecification~T~ <|.. IExpressionSpecification~T~
    IExpressionSpecification~T~ <|.. Specification~T~
    Specification~T~ <|-- LambdaSpecification~T~
    Specification~T~ <|-- CompositeSpecification~T~
    Specification~T~ <|-- NegatedSpecification~T~
```

---

## 7. Error Handling Flow

```mermaid
flowchart TB
    A["QuerySpec.Where(null)"] --> B["ArgumentNullException"]
    C["QuerySpec.Page(0, pageSize)"] --> D["ArgumentOutOfRangeException"]
    E["QuerySpec.Skip(-1)"] --> F["ArgumentOutOfRangeException"]
    G["Spec.Between(lower:50, upper:10)"] --> H["ArgumentException"]
    I["Translator.Translate(unsupported expr)"] --> J["NotSupportedException"]
    K["repo.SingleOrDefaultAsync() - multiple matches"] --> L["InvalidOperationException"]
    M["IReadRepository null result"] --> N["ReadRepositoryResultExtensions\nResult Error.NotFound"]
```

---

## 8. Clean Architecture Integration

```mermaid
graph TB
    subgraph Domain["Domain Layer"]
        E["Entity: Customer"]
        S1["ActiveCustomerSpecification"]
        S2["VipCustomerSpecification"]
    end

    subgraph Application["Application Layer (CQRS)"]
        H["GetTopCustomersHandler"]
        R["IReadRepository&lt;Customer&gt;"]
    end

    subgraph Infrastructure["Infrastructure Layer"]
        EF["EfReadRepository&lt;TCtx,Customer&gt;"]
        DP["Dapper + QuerySpecTranslator&lt;Customer&gt;"]
        MG["MongoSpecificationEvaluator"]
    end

    subgraph Presentation["Presentation Layer"]
        API["Controller / gRPC / Worker"]
    end

    API --> H
    H --> R
    H --> S1
    H --> S2
    R --> EF
    R --> DP
    R --> MG
```

---

## 9. QueryPlanCache States (LRU)

```mermaid
stateDiagram-v2
    [*] --> Empty
    Empty --> Populated : SetPlan
    Populated --> CacheHit : TryGetPlan found
    Populated --> CacheMiss : TryGetPlan not found
    CacheMiss --> Populated : SetPlan new entry
    Populated --> Evicted : Capacity exceeded LRU
    Evicted --> Populated : SetPlan replaces LRU entry
    Populated --> Empty : Clear()
    CacheHit --> Populated
```

---

## 10. In-Memory Evaluation Flow (AOT-Safe)

```mermaid
flowchart TB
    A["Specification.IsSatisfiedBy(candidate)"]
    B["ExpressionInterpreter.Evaluate(expr, candidate)"]
    C{"Expression NodeType?"}
    D["BinaryExpression AND/OR/NOT -> recurse"]
    E["MemberExpression -> property access"]
    F["ConstantExpression -> return value"]
    G["MethodCallExpression -> dispatch table"]
    H["bool result"]

    A --> B --> C
    C --> D --> C
    C --> E --> H
    C --> F --> H
    C --> G --> H
```
