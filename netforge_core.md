# NetForge Core Toolkit Documentation

Version: 0.2 (Living Document)

Changelog 0.2:
 
* Renamed references from ".NET Forge" to "NetForge" consistently.
* Clarified multi-ORM repository + UnitOfWork support (EF Core primary, optional Dapper/Linq2Db adapters).
* Added domain event post-commit dispatch concept to pipeline section.
* Added upcoming behaviors (Logging, Caching, Timing) placeholders.
* Added client generation philosophy (Refit / Kiota / manual) and resilience layering mention.

## 1. Introduction

The NetForge Core library is the foundational toolkit for the Fused Slice Architecture (see rule catalog in `fused_slice_architecture.md`). It underpins rules FSA-01 (Feature Fusion), FSA-02 (Thin Hosts), FSA-04 (Explicit Results), FSA-06 (Repository + UoW), FSA-07 (Post-Commit Events), FSA-08 (Specifications), FSA-10 (Behavior Pipeline), FSA-11 (Validation Short-Circuit), FSA-15 (Protocol Agnosticism). It is a source-included, dependency-light library providing simple, robust, in-house implementations of common patterns required for modern application development.

The philosophy is **empowerment through ownership**. By including these patterns as source code rather than an external NuGet package, you have full control to understand, customize, and extend them to fit your exact needs.

## 2. Setup and Dependency Injection

The toolkit's services are registered via a single extension method, typically called from your Presentation host's startup configuration.

```csharp
// In Program.cs
builder.Services.AddNetForgeCore();
builder.Services.AddInfrastructureData();
```

The `AddForge()` method scans your assemblies to automatically register all handlers, validators, and pipeline behaviors.

## 3. Core Patterns

### Result Pattern (`Result` and `Result<TValue>`)

* **Purpose (FSA-04):** Handle success and failure explicitly without exceptions for control flow.
* **Philosophy:** Inspired by FluentResults-like semantics; can hold multiple errors; enforces explicit handling.
* **Usage Example:**

```csharp
    // In a handler:
    public async Task<Result<ProductDto>> Handle(GetProductById request, ...)
    {
        var product = await _repository.GetByIdAsync(request.Id);
        if (product is null)
        {
            // Return a failure Result
            return Result<ProductDto>.Failure(new Error("Product.NotFound", "Product not found."));
        }
        var dto = ForgeMapper.Map<Product, ProductDto>(product);
        // Return a success Result
        return Result.Success(dto);
    }

    // In the API endpoint:
    return result.Match(
        onSuccess: productDto => Results.Ok(productDto),
        onFailure: errors => Results.NotFound(errors)
    );
```

### Mediator Pattern (`Mediator`, `IRequest<T>`, `IRequestHandler<T, U>`)

* **Purpose (FSA-10, FSA-15):** Decouple sender from handler (CQRS-friendly, protocol agnostic).
* **Philosophy:** Simple DI-based dispatcher orchestrating pipeline behaviors.
* **Usage Example:**

```csharp
    // In a Blazor component:
    [Inject] private Mediator Mediator { get; set; }

    private async Task CreateProduct()
    {
        var command = new ProductsFeature.CreateProduct("New Widget", 10.0m);
        var result = await Mediator.Send(command);
        // ... handle result
    }
```

### Pipeline Behaviors (`IPipelineBehavior<T, U>`)

* **Purpose (FSA-10):** Handle cross-cutting concerns (validation, transactions, logging, caching, timing) via decorator pipeline.
* **Philosophy:** Small, focused behaviors compose sequentially. Current behaviors:
    * `ValidationBehavior` (FSA-11): Validates requests with a matching `ForgeValidator` (short-circuit on failure).
    * `UnitOfWorkBehavior` (FSA-06, FSA-07): Encloses command handlers in a transaction, defers `CommitAsync` until post-handler success, then triggers post-commit domain event dispatch.
* **Planned:** `LoggingBehavior`, `CachingBehavior`, `TimingBehavior` (future phases).

### Validation Pattern (`ForgeValidator<T>`, `IValidator<T>`)

* **Purpose (FSA-11):** Provide a simple, explicit way to define validation rules for commands.
* **Philosophy:** Avoid a complex fluent API; validators are nested within the request type.
* **Usage Example:**

```csharp
    public sealed record CreateProduct(string Name, decimal Price) : IRequest<Result<Guid>>
    {
        public class Validator : ForgeValidator<CreateProduct>
        {
            protected override void OnValidate(CreateProduct instance)
            {
                RuleFor(() => string.IsNullOrWhiteSpace(instance.Name), new("Name", "Name is required."));
                RuleFor(() => instance.Price <= 0, new("Price", "Price must be positive."));
            }
        }
        // ... Handler ...
    }
```

### Domain Patterns (`ValueObject`, `SmartEnum`, `Specification<T>`)

* **Purpose (FSA-05, FSA-08):** Enable rich, robust Domain-Driven Design with specification-driven querying.
* **Philosophy:** Base classes provide boilerplate for powerful domain primitives.
    * `ValueObject`: Immutable types defined by their attributes (e.g., `Money`).
    * `SmartEnum`: Strongly-typed enumerations containing logic (e.g., `OrderStatus`).
    * `Specification<T>`: Encapsulates reusable query logic (filter, include, order, paging) composably.
* **Usage Example (Specification):**

```csharp
    // In a handler:
    var activeSpec = new ActiveProductSpecification();
    var featuredSpec = new FeaturedProductSpecification();
    var products = await _repository.FindAsync(activeSpec.And(featuredSpec));
```

### Mapping (`ForgeMapper`)

* **Purpose:** Provide simple, convention-based object-to-object mapping (supports FSA-01 by keeping feature mapping local & lightweight).
* **Philosophy:** Lightweight reflection / expression-based property copier with future caching optimization. Intended for straightforward DTO projections (deep or conditional mapping left to feature code or manual mapping for clarity).
* **Usage Example:**

```csharp
    // In a handler:
    var product = await _repository.GetByIdAsync(id);
    var dto = ForgeMapper.Map<Product, ProductDto>(product);
    return Result.Success(dto);
```

### Repositories & Unit of Work

* **Repositories (FSA-03, FSA-06):** Thin abstractions over persistence enabling specification-driven querying and multi-provider flexibility (EF Core, Dapper, Linq2Db). Handlers depend on interfaces only.
* **UnitOfWork (FSA-06, FSA-12):** Coordinates transaction + post-commit domain event dispatch. Exposed via `IUnitOfWork` (CommitAsync). Implementation strategy may switch between native EF transactions and `TransactionScope` for multi-provider consistency; repositories never commit.
* **Specifications (FSA-08):** Composable filters/includes/order/paging objects passed to repositories; provider-specific evaluators translate them.

### Domain Events

* **Raising:** Entities add domain events during state changes.
* **Dispatch (FSA-07):** `UnitOfWorkBehavior` collects and dispatches events after a successful commit to avoid side-effects on failed transactions.
* **Handlers:** Regular mediator notification handlers or dedicated domain event dispatcher within Core.

### Client Generation Philosophy (External Calls)

* **Options (FSA-13):** Refit (concise interface -> implementation), Kiota (OpenAPI -> strong client), Manual HttpClient wrappers (maximum control).
* **Resilience:** Applied via composition (Polly policies) around generated or manual clients—not baked into handlers.
* **Guideline:** Start simple (manual/refit), adopt Kiota when a stable OpenAPI contract exists.

---

### Cross-Document Navigation

[Architecture Rules](./fused_slice_architecture.md) | [Design Phases](./netforge_core_design.md) | [AI Prompt](./ai_prompt.md) | [Repository Readme](./readme.md)

