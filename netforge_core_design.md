# NetForge Core Toolkit – Development Design Document

Version: 0.2
Date: 2025-08-16
Status: Draft (open for collaborative refinement)

---

## 1. Purpose & Vision

NetForge Core provides a lean, fully in-house, dependency-free application toolkit underpinning the Fused Slice Architecture (FSA). It supplies the primitives, patterns, and infrastructure glue (Mediator + Pipeline + Result + Validation + Specification + Domain Events + Mapping + Guards, etc.) required to build **feature-centric**, **thin-host**, **imperative/OOP-style** .NET applications without external functional libraries.

Rule Alignment: Supports architecture rules FSA-01 (Feature Fusion), FSA-02 (Thin Hosts), FSA-03 (Abstraction Dependency), FSA-04 (Explicit Results), FSA-06 (Repository + UoW), FSA-07 (Post-Commit Dispatch), FSA-08 (Specifications), FSA-10 (Behavior Pipeline), FSA-11 (Validation Short-Circuit), FSA-12 (Repos Don't Commit), FSA-15 (Protocol Agnostic Features). See `fused_slice_architecture.md`.

Cross-Document Navigation: [Architecture Rules](./fused_slice_architecture.md) | [Toolkit](./netforge_core.md) | [AI Prompt](./ai_prompt.md) | [Readme](./readme.md)

Design goals:

- Deterministic, explicit control flow (no fluent functional monads / chain-heavy APIs).
- Small surface, high clarity; easy to read in single files (supports AI agents & humans).
- Extensible via interfaces + pipeline behaviors; never force inheritance where composition suffices.
- Source-level ownership: everything is internalizable, debuggable, modifiable.
- Non-alloc conscious for hot paths (later optimization passes; start simple + correct).

Non-goals (explicitly avoided):

- Fluent/monadic extension chains (Then/Select/Bind) reminiscent of VoidCore functional style.
- Hidden reflection magic beyond initial scanning & simple mapper property copy.
- Implicit global state.

---

## 2. Pattern Inventory (Baseline vs Planned Enhancements)

| Pattern / Facility | Baseline Scope | Planned Enhancements | Notes (Differences from VoidCore) |
|--------------------|----------------|----------------------|-----------------------------------|
| Result / Result&lt;T&gt; | Success/Failure + Errors collection | Aggregation (Combine), Domain error taxonomy, Serialization adapters | No fluent Then/Select; consumers branch explicitly. |
| Error Model | `Error` (Code, Message, Metadata?) | Error categories (Validation/Conflict/NotFound) | Map cleanly to HTTP in thin hosts. |
| Guard Clauses | Static `Guard` class + extension helpers | Lazy message factories, batch evaluation | VoidCore has EnsureX; we supply minimal core first. |
| Mediator | IRequest, IRequestHandler, IMediator | Streaming requests (IStreamRequest), Notification publish (INotification) | Keep simple; add notifications after base pipeline is stable. |
| Pipeline Behaviors | Validation, UnitOfWork | Logging, Performance timing, Caching stub, Exception translation | Ordering contract defined. |
| Validation | `ForgeValidator<T>` imperative rules | Cross-property rules, async validation hook | No fluent DSL builder; rule = predicate + error. |
| Specification | Base Specification + And/Or/Not combinators | EF Core expression adaptation, Include spec / ordering spec types | Provide translation helpers separate from core. |
| Mapping | Reflection property copy (opt-in) | Compile-time expression cache, Custom profile registry | Intentionally minimal vs AutoMapper. |
| Domain Base | Entity, ValueObject, SmartEnum | AggregateRoot marker, Concurrency token helper, SoftDelete interface | Domain events integrated at Entity base. |
| Domain Events | Simple interface + dispatcher | Scoped buffering + post-commit publication | Mediator facilitates both command/query & event publishing. |
| Messaging (Internal) | `IDomainEvent`, `INotification` | Event-to-Integration mapping hook | Distinguish domain vs integration events. |
| Repositories | Optional abstraction per aggregate root | Generic + ORM-specific adapters (EF Core, Dapper, Linq2Db) | Encouraged when persistence logic non-trivial; can bypass for simple queries. |
| Unit of Work | Abstraction w/ CommitAsync + BeginScope | TransactionScope adapter, ambient context, multi-DB coordination | Enables mixing EF Core + Dapper + Linq2Db in single logical operation. |
| Data Context Adapters | EFCoreContextAdapter, DapperConnectionAdapter | Bulk operations, batching strategies | UoW composes one or more context adapters. |
| Specification Evaluator | Compose expressions + includes | Provider-specific translation (EF, LINQ2DB), projection | Keeps specs provider-agnostic; adapters translate. |
| Guarded Collections | N/A initially | Add `ReadOnlyValueCollection<TValueObject>` helper | Deferred. |
| Caching Facade | Interface placeholder | MemoryCache adapter (optional) | Late-stage, only if needed. |
| Logging Hook | Abstraction only | Behavior injecting ILogger&lt;TRequest&gt; | No logging implementation here. |
| Configuration Options | `ForgeOptions` | Validation of options | Light optional config class. |
| Assembly Scanning | Handler + Validator + Behavior registration | Attribute filtering, opt-out markers | Minimal first; optimize if perf concern. |
| Hosting (Minimal APIs) | Thin endpoint mapping to mediator | Endpoint grouping, versioning helpers | No base controllers; extension methods only. |
| gRPC Support | Optional service layer adapters | Interceptors for validation, tracing | Handlers remain protocol-agnostic. |
| Aspire Orchestration | Optional environment compose | Health, tracing, service discovery | Toolkit independent, opt-in examples. |
| API Client Generation | Manual HTTP | Refit/Kiota integration helpers | Developer chooses per service. |
| Client Resilience | Polly policy injection hooks | Circuit-breaker, retry profiles | Not baked into handlers. |

---

## 3. High-Level Architecture (Internal)

```text
+-------------------------+
|      IMediator          |
|  - Resolves handler     |
|  - Builds pipeline      |
+-----------+-------------+
            |
   +--------v---------+    Behaviors wrap next (decorator chain)
        | Pipeline Chain   |--> ValidationBehavior
        | (Func<Task<T>>)  |--> Custom Behaviors (Logging, Timing, etc.)
        |                  |--> UnitOfWorkBehavior (commands)
   +--------+---------+
            |
      +-----v------+
      | Handler(s) |
      +-----+------+
            |
     (Repositories / Guards / Domain Events)
```

Domain Events Flow (planned, Unit of Work centric):

1. Handler uses repositories or direct context adapters via injected abstractions to load/mutate aggregates; aggregates call `AddDomainEvent()`.
2. Handler returns `Result` (success/failure) without committing.
3. `UnitOfWorkBehavior` (FSA-06, FSA-12) (post-handler on success) invokes `IUnitOfWork.CommitAsync()`; underlying implementation may:
    - Use native EF Core transaction
    - Use `TransactionScope` to coordinate multiple connections (e.g., EF + Dapper)
    - Use provider-specific transactions (e.g., Npgsql, SqlConnection) aggregated.
4. After successful commit (FSA-07), behavior gathers domain events from tracked entities (via repository/context adapters), clears them, publishes as notifications.
5. Notification handler failures are isolated (logged) and do not roll back the already committed transaction (configurable future option for outbox pattern).

---

## 4. Core Public API Sketch (Illustrative, Non-Final)

(See existing `netforge_core.md` for foundational patterns; this doc adds extended surface.)

---

## 4a. Hosting & Protocol Strategy

Objectives:

- Provide multiple protocols (HTTP Minimal APIs, gRPC) without coupling domain or feature logic.
- Keep startup friction low: a developer can expose a feature with 2–3 lines in Program.cs.

Approach:

- Minimal API: feature-focused extension method `MapMealPlannerEndpoints(this RouteGroupBuilder group)`.
- gRPC: generated service inherits from proto base, delegates to mediator; no business logic in the service class.
- Shared Result Mapping: centralized `HttpResultMapper` converting `Result` / `Result<T>` to `IResult` / ProblemDetails.
- Aspire: sample `AppHost` project wiring OpenTelemetry, health checks, service references; entirely optional.

Interceptors (gRPC):

- Validation pass-through (ensures mediator pipeline handles validation; interceptor can convert unexpected exceptions to gRPC status codes).
- Tracing interceptor adds activity tags (method, feature, success flag).

Versioning Plan:

- Route groups per version: `app.MapGroup("/v1").MapMealPlannerEndpoints();`
- gRPC versioning via package / proto namespace increments.

---

## 4b. Client Generation & Consumption

Modes Supported:

1. Manual: Direct `HttpClient` usage + small wrapper class.
2. Refit: Interface per external service; registered with resilience policies (Polly).
3. Kiota: Generated from OpenAPI exported by minimal API; partial classes extend domain-specific conversions.

DI Helpers (planned):

```csharp
services.AddRefitClientWithPolicies<IMealPlannerApi>(builder => { /* optional RefitSettings */ }, PolicyProfiles.StandardRetry());
services.AddKiotaClient<IMealPlannerClient>(httpFactory: sp => sp.GetRequiredService<HttpClient>());
```

Resilience Profiles:

- Provided as static factory methods returning arrays of `IAsyncPolicy<HttpResponseMessage>`.
- Opt-in; no automatic registration.

Error Normalization:

- Client adapters convert remote errors (JSON ProblemDetails, validation arrays) into `Result` with appropriate `ErrorCategory`.

Documentation will include a decision matrix guiding selection (stability, change frequency, language interop needs).

---

### 4.1 Result Enhancements

```csharp
public static class ResultExtensions // minimal, non-functional style
{
    public static Result Combine(params Result[] results) { /* iterate, aggregate errors */ }
    public static Result<T> Combine<T>(params Result<T>[] results) { /* ... */ }
}
```

Aggregation stays explicit; no chain transformation helpers.

### 4.2 Guard Clauses

```csharp
public static class Guard
{
    public static void AgainstNull<T>(T? value, string name) where T : class { /* throw ArgumentNullException */ }
    public static void AgainstNullOrEmpty(string? value, string name) { /* ... */ }
    public static void AgainstOutOfRange(bool condition, string message) { if (condition) throw new ArgumentOutOfRangeException(message); }
}
```

Used internally inside toolkit and available for domain.

### 4.3 Mediator Notifications & Streams (Phase 2)

Interfaces:

- `INotification` marker.
- `INotificationHandler<TNotification>`.
- `IStreamRequest<TResponse>` + `IStreamRequestHandler<TRequest, TResponse>` (optional / deferred).

### 4.4 Domain Events (Phase 2)

```csharp
public interface IDomainEvent { DateTime OccurredOn { get; } }
public interface IDomainEventDispatcher { Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken ct); }
```

`Entity` accumulates domain events. Dispatcher implementation lives in Infrastructure; core provides interface + pipeline hook.

### 4.5 Specifications Extensions

Additional types:

- `OrderBySpecification<T>` (key selector + ascending/descending)
- `IncludeSpecification<T>` (string or expression includes – EF-specific bridging stays outside core or in extension assembly)
- `PagedSpecification<T>` (page, size) – purely metadata wrapper consumed by repository adapter.

### 4.6 Mapping Profiles (Optional Future)

Introduce `IForgeMappingProfile` with `Configure(ForgeMapperConfiguration cfg)` enabling custom property-level binds. Base mapper remains reflection-first fallback.

---

## 5. Error Classification Strategy

Error categories (enum or static codes):

- Validation
- NotFound
- Conflict
- Unauthorized
- Forbidden
- Infrastructure (transient)

Mapping logic for HTTP is handled in Presentation, but consistent codes enable generic translation.

`Error` optional metadata bag: `Dictionary<string, object?>` (deferred until needed).

---

## 6. Pipeline Ordering & Contracts

Default registration order (top executes first):

1. `ExceptionHandlingBehavior` (future) – translate unexpected exceptions to logged failures.
2. `LoggingBehavior` (future) – timing + correlation.
3. `ValidationBehavior` (FSA-11) – short-circuit on validation errors.
4. `CachingBehavior` (future) – queries only (`IQuery<T>`).
5. `UnitOfWorkBehavior` (FSA-06, FSA-07, FSA-12) – commands only (`ICommand` / `ITransactionalRequest`); executes handler, then commits via `IUnitOfWork.CommitAsync()`; publishes domain events post-commit.

Marker Interfaces Plan:

- `ICommand` / `ICommand<Result>` / `ICommand<Result<T>>`
- `IQuery<Result<T>>`
- `ITransactionalRequest` (optional explicit marker; otherwise all commands are transactional)

UnitOfWork implementations may internally choose TransactionScope vs provider transaction depending on configuration (e.g., `ForgeUnitOfWorkOptions.UseTransactionScope = true`).

---

## 7. Configuration & Extensibility

`ForgeOptions`:

```csharp
public sealed class ForgeOptions
{
    public bool EnableAssemblyScanning { get; set; } = true;
    public bool ThrowOnUnhandledRequest { get; set; } = true;
    public bool AutoRegisterNotificationHandlers { get; set; } = true;
}
```

`AddForge(Action<ForgeOptions>? configure = null)` merges defaults, registers based on options.

Extension Points:

- Custom pipeline behaviors (add before/after defaults).
- Replacement of mapper via `IObjectMapper` registration.
- Guard library internal but open for extension by additional static partial classes.

---

## 8. Performance Considerations (Roadmap)

Phase 1: Simplicity first, measure later.
Phase 2: Cache reflection for:

- Handler method resolution (dictionary keyed by request type).
- Mapper property copy delegates (Expression.Compile once).
- Validator instance reuse (stateless; keep scoped or singleton if safe).

Phase 3: Introduce optional Source Generator for handler registry (if scanning becomes a bottleneck).

---

## 9. Testing Strategy

Test Pyramid (toolkit scope):

- Unit: Result, Guard, Mapper, Validator, Mediator dispatch, Pipeline short-circuit logic.
- Integration-lite: Pipeline composition ordering (via in-memory DI container), Domain event dispatch sequence.
- Performance micro-bench (later): Mapper vs manual mapping comparison (BenchmarkDotNet optional external).

Highly focused tests ensuring no reliance on external libs; all deterministic.

---

## 10. Documentation Artifacts

- `netforge_core.md`: High-level user-facing toolkit overview.
- `netforge_core_design.md` (this file): Internal design & implementation plan.
- `CHANGELOG.md`: Semantic, starting when API stabilizes.
- XML doc summaries across public APIs.

---

## 11. Incremental Git Commit Plan

Each commit: small, reviewable, builds green. Suggested commit messages (imperative mood). Grouped by phases.

### Phase 0: Scaffolding

1. feat(core): add NetForge.Core project targeting net9.0 with initial Directory.Build.props
2. chore(core): add EditorConfig + nullable + analyzers configuration
3. docs(core): add README stub & update design document reference

### Phase 1: Core Primitives

1. feat(result): implement Result & Result&lt;T&gt; with Error model
2. test(result): add unit tests for success/failure/aggregate
3. feat(guards): introduce Guard static helpers (null, empty, range)
4. test(guards): unit tests for guard clauses

### Phase 2: Mediator Foundation

1. feat(mediator): add IRequest, IRequestHandler, IPipelineBehavior, IMediator, Mediator basic dispatch
2. test(mediator): verify handler invocation and unknown request exception
3. feat(pipeline): implement pipeline construction and execution order
4. test(pipeline): ensure behaviors wrap correctly (ordering test)

### Phase 3: Validation

1. feat(validation): add ForgeValidator&lt;T&gt;, ValidationError, IValidator&lt;T&gt;
2. feat(behavior): add ValidationBehavior with Result short-circuit
3. test(validation): validator success/failure & pipeline short-circuit tests

### Phase 4: Unit of Work & Transactional Separation

1. feat(unitofwork): introduce IUnitOfWork + CommitAsync + BeginTransaction semantics
2. feat(unitofwork): implement EfCoreUnitOfWork (single DbContext) + TransactionScopeUnitOfWork strategy switch
3. feat(repos): add generic repository abstraction (IRepository&lt;T&gt;) + basic EF implementation
4. test(unitofwork): commit on success, no commit on failure

### Phase 5: Domain Events Dispatch Pipeline

1. feat(events): extend Entity base with domain events list + AddDomainEvent
2. feat(events): add IDomainEvent interface & dispatcher abstraction
3. feat(behavior): add PostCommitDomainEventsBehavior (or integrate into UnitOfWorkBehavior configurable) publishing events after commit
4. test(events): events published only when commit succeeds

### Phase 6: Specifications

1. feat(spec): base Specification&lt;T&gt; + And/Or/Not composites
2. feat(spec): ordering / paging metadata structs
3. test(spec): expression composition tests

### Phase 7: Mapping

1. feat(mapper): add ForgeMapper reflection-based engine
2. perf(mapper): add property accessor caching dictionary
3. test(mapper): basic mapping + ignored mismatched types

### Phase 8: Notifications & Integration Events

1. feat(notification): add INotification + handler + mediator publish
2. test(notification): ensure multiple handlers invoked
3. feat(events): optional translation layer DomainEvent -> Notification (adapter)

### Phase 9: Options & DI

1. feat(di): implement AddForge with assembly scanning (handlers, validators, behaviors)
2. feat(options): introduce ForgeOptions & configuration delegate
3. test(di): scanning registration integration test

### Phase 10: Error Classification & HTTP Mapping Support

1. feat(error): add ErrorCategory enum + mapping helper (no ASP.NET dependency)
2. docs(core): update toolkit docs with error categories

### Phase 11: Logging & Timing Behavior (Optional Early Optimization)

1. feat(behavior): add LoggingBehavior interface (no concrete logger dependency beyond ILogger&lt;T&gt;)
2. feat(behavior): add TimingBehavior capturing elapsed ms (debug only)
3. test(behavior): verify ordering relative to validation & uow

### Phase 12: Caching (Deferred / Optional)

1. feat(caching): placeholder ICachingProvider + CachingBehavior (queries only)
2. docs(design): mark caching experimental

### Phase 13: Cleanup & Quality

1. chore(analysis): enable CA + Roslyn analyzers; fix warnings
2. docs(changelog): add initial CHANGELOG with implemented phases
3. refactor(core): minor API polish based on review feedback

### Phase 14: Stabilization

1. test(api): add regression tests for previously reported issues (placeholder)
2. docs(core): expand README with usage examples for each pattern
3. chore(version): tag v0.1.0 pre-release

---

Rule Reference Quick Map: Pipeline (FSA-10,11,06,07,12), Domain Events (FSA-07), Specifications (FSA-08), Thin Hosts (FSA-02), Feature Fusion (FSA-01).


(Adjust / reorder collaboratively as needed.)

### Phase 15: Hosting (Minimal APIs)

1. feat(host): add endpoint mapping extensions + Result->IResult mapper
2. docs(host): minimal API quickstart
3. test(host): integration test verifying error mapping to ProblemDetails

### Phase 16: gRPC Integration (Optional)

1. feat(grpc): add sample proto + generated service adapter to mediator
2. feat(grpc): add validation + tracing interceptors
3. test(grpc): unary roundtrip feature test

### Phase 17: Aspire Orchestration (Optional)

1. feat(aspire): add sample AppHost project (OpenTelemetry, health checks)
2. docs(aspire): usage and opt-in instructions

### Phase 18: Client Generation & Resilience

1. feat(client-refit): Refit registration helper + sample interface
2. feat(client-kiota): Kiota adapter + generation guide
3. feat(client-polly): standard policy profile helpers (retry, circuit-breaker)
4. test(client): ensure policy pipeline executes + error normalization

---

## 12. Risk & Mitigation

| Risk | Impact | Mitigation |
|------|--------|-----------|
| Over-scoping early commits | Slow initial progress | Strict phase boundaries & review gates |
| Reflection performance | Latency in hot paths | Cache delegates (Phase 2/mapper perf) |
| Domain event dispatch timing | Duplicate or missed events | Centralize in one behavior after commit |
| Silent handler resolution failures | Runtime errors | Option `ThrowOnUnhandledRequest` + unit tests |
| Validation coupling to Result shape | Inflexibility later | Keep behavior isolated; allow adapter later |

---

## 13. Open Questions (for Collaboration)

1. Do we need streaming requests (IAsyncEnumerable) early? (Default: defer.)
2. Domain events dispatch pre- vs post-commit? (Current: post-commit ensures consistency.)
3. Should Error metadata dictionary be included from start or postponed? (Default: postpone.)
4. Provide synchronous validator support only or add async rules? (Async deferred.)

---

## 14. Immediate Next Step

Proceed with Phase 0 Commit 1–3 (scaffold project + baseline docs) once this design is approved or amended.

---

## 15. Change Log (Design Doc Only)

- 0.1: Initial comprehensive design & phased implementation plan.

---

## 16. Collaboration Guidelines

- Keep commits < 300 lines when feasible.
- Favor additive commits; defer refactors to dedicated refactor commit buckets.
- Every new public type must have XML doc summary.
- New behaviors require an ordering test.

---

Feedback welcome—propose edits inline or via follow-up tasks. Next action upon approval: start Phase 0 scaffolding.
