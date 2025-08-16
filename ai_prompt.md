# AI Build Prompt – MealPlanner (FSA)
Version: 0.2 (Living Document)

Hello. Your task is to act as a senior .NET developer and architect, specializing in the **Fused Slice Architecture (FSA)** and the **NetForge Core Toolkit** (an evolution of the .NET Forge concepts). You will build a new web application from scratch, strictly adhering to the updated principles and patterns of this architecture.

**Primary Goal:**
Build a "MealPlanner" application, a simplified clone of the "FoodStuffs" project. This application will allow users to manage grocery items and plan meals.

## Core Concepts & Constraints (You MUST follow these)

Refer to rule catalog in `fused_slice_architecture.md` for IDs.

1. **Architecture (FSA-01, FSA-15):** Use the Fused Slice Architecture. Standard projects: `Core`, `Domain`, `Features`, `Infrastructure`, `Presentation` (API / optional gRPC / optional Aspire host), optional `Clients`.
2. **Toolkit (FSA-14):** Use in-house NetForge Core patterns (Mediator, Result, Validation, Specification, ValueObject, UnitOfWork, Mapping) unless a gap is proven.
3. **Feature Fusion (FSA-01):** All logic for a feature (commands, queries, handlers, DTOs, validators, internal mappers) resides in a single `[FeatureName]Feature.cs` file.
4. **Thin Hosts (FSA-02):** Presentation projects are transport adapters (Minimal APIs, gRPC). They just translate inputs to mediator requests and map `Result` out.
5. **Error Handling (FSA-04):** Handlers always return `Result` / `Result<T>`; no exceptions for control flow. Failure categories map to HTTP/gRPC status codes.
6. **Persistence Flexibility (FSA-03, FSA-06, FSA-12):** Use repository + unit of work abstractions. EF Core default; allow mixing (EF Core + Dapper/Linq2Db). No direct `DbContext` usage in handlers.
7. **Domain Events (FSA-07):** Domain events dispatched only after successful commit by `UnitOfWorkBehavior`.
8. **Specifications (FSA-08):** Queries use composable specifications passed to repositories.
9. **Clients (FSA-13):** Outgoing service calls via Refit, Kiota, or manual HttpClient wrappers with centralized resilience.
10. **Composition Over Inheritance (FSA-09):** Use extension methods & small interfaces; base classes only for core primitives (ValueObject, Entity).

## Application to Build: "MealPlanner"

### Domain Model

* `GroceryItem`:
    * `Id` (Guid)
    * `Name` (string)
    * `Category` (string) - e.g., "Dairy", "Produce", "Meat"
    * `Quantity` (int)
    * `IsAcquired` (bool)
* `Meal`:
    * `Id` (Guid)
    * `Name` (string)
    * `Ingredients` (a collection of `GroceryItem` entities)

## Step-by-Step Implementation Plan

Please proceed step-by-step. I will review each step.

### Step 1: Project & Core Setup
1.  Create solution structure: `NetForge.Core`, `MealPlanner.Domain`, `MealPlanner.Features`, `MealPlanner.Infrastructure`, `MealPlanner.Presentation.Api`, optional `MealPlanner.Presentation.Grpc`, optional `MealPlanner.AppHost` (Aspire), `MealPlanner.Clients`.
2.  Implement NetForge Core primitives: `Result`, `Error`, `Mediator`, `Pipeline` (Validation, UnitOfWork), `ForgeValidator`, `Specification` combinators, `Entity`, `ValueObject`, `SmartEnum`, `ForgeMapper`, `IUnitOfWork`, `IRepository<T>`, domain event interfaces.
3.  Add DI extension `AddNetForgeCore()` scanning assemblies.

### Step 2: Implement the Domain Layer
1.  Define entities: `GroceryItem`, `Meal` (+ domain events if needed).
2.  Define repositories: `IGroceryItemRepository`, `IMealRepository` (CRUD + spec query methods: `Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec)` etc.).
3.  Define `IUnitOfWork` abstraction (CommitAsync, BeginScope if needed).
4.  Create shared specifications (e.g., `GroceryItemsByCategorySpec`).

### Step 3: Implement the `GroceryItemsFeature` Fused Slice
1.  Create `GroceryItemsFeature.cs` (DTOs, commands, queries, validators, handlers fused).
2.  Implement: Create, GetById, ListAll (with optional spec filters), Update (quantity, toggle acquired), Delete.
3.  Use repository interfaces + `IUnitOfWork` (commit handled by behavior; no SaveChanges in handler).
4.  Map entities to DTOs using `ForgeMapper` or manual mapping inside the feature file.

### Step 4: Implement the Infrastructure Layer
1.  Add EF Core DbContext + entity configurations.
2.  Implement repositories (EF variant) + optional Dapper adapter example.
3.  Implement UnitOfWork (EF transaction + optional TransactionScope strategy for multi-provider scenarios).
4.  Implement Specification evaluator for EF translation.
5.  Register infrastructure via `AddInfrastructureData()` (context, repos, UoW, evaluator).

### Step 5: Implement the `Api` Host (Presentation / Minimal APIs)
1.  Add `Program.cs` configuring NetForge Core + Infrastructure.
2.  Add endpoint mapping extension `MapGroceryItemEndpoints` (POST, GET all, GET by id, PUT, DELETE) using mediator.
3.  Implement unified Result -> `IResult` mapping (success codes, error ProblemDetails).
4.  Add versioned route group `/v1`.

### Step 6: Implement Testing
1.  Unit tests: Result, validators, handlers (mock repos/UoW with substitutes).
2.  Integration tests: Minimal API endpoints (Testcontainers for Postgres/SQL Server + Respawn reset).
3.  Domain event dispatch test (ensure post-commit behavior).
4.  Optional: performance micro-bench for mapping (later).

### Optional Extensions (Later Phases)
* gRPC host + interceptors.
* Aspire AppHost instrumentation.
* Refit / Kiota client generation example project.

Please begin with **Step 1**. I am ready to review your work.

---

Cross-Document Navigation: [Architecture Rules](./fused_slice_architecture.md) | [Toolkit](./netforge_core.md) | [Design Doc](./netforge_core_design.md) | [Readme](./readme.md)
