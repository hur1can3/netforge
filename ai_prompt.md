# AI Build Prompt – FoodForge (FSA)

Version: 0.3 (Living Document)

Hello. You are an expert .NET 9 developer and architect specializing in **Fused Slice Architecture (FSA)** and the **NetForge Core Toolkit**. You will build the sample application described in `application_spec_foodforge.md` (FoodForge specification) strictly following FSA rules (FSA-01..25) plus new optional advanced practices (Outbox, Telemetry, Concurrency, Idempotency, Paging, Caching). Implementation must remain source-owned unless a justified gap is documented.

**Primary Goal:**
Implement the FoodForge domain: Recipes, Ingredients, Meal Plans, Shopping Lists, Tags, Ratings, Pantry, Search, and Image Processing (stubbed) with explicit Results and post-commit domain events.

## Core Concepts & Constraints (STRICT)

See `fused_slice_architecture.md` rule catalog.

1. **Fused Feature Files (FSA-01):** Each feature = single `[FeatureName]Feature.cs` containing requests, validators, handlers, internal mappers, DTOs.
2. **Thin Adaptation (FSA-02, FSA-15):** Hosts map transport ⇄ mediator; no domain/persistence logic.
3. **Explicit Results (FSA-04):** Return `Result` / `Result<T>`; no exceptions for expected flows; map errors via taxonomy in spec.
4. **Abstractions & UoW (FSA-03,06,12):** Handlers depend only on repository + UoW abstractions; no `SaveChanges`/transactions inline.
5. **Post-Commit Events (FSA-07):** Domain events raised by entities, published after commit; Outbox for external integration (FSA-18).
6. **Specifications (FSA-08):** All queries & paging via specs; no inline provider queries.
7. **Composition First (FSA-09):** Prefer small interfaces / extensions over inheritance.
8. **Pipeline Behaviors (FSA-10,11):** Validation, UnitOfWork (commit + event dispatch), optional Caching/Logging/Timing.
9. **Structured Telemetry (FSA-16):** Every handler logs structured template with feature_name, success, duration_ms. Activities around external calls.
10. **Idempotency (FSA-17):** External side-effect commands require idempotency key (e.g., image uploads).
11. **Outbox (FSA-18):** Integration events & search index updates go through Outbox + dispatcher service.
12. **Caching (FSA-19):** Apply via decorators for read-heavy specs (search facets) not inline.
13. **Paging & Bounds (FSA-20):** All collection endpoints require page + size (max 100).
14. **Optimistic Concurrency (FSA-21):** Update commands check concurrency token -> Conflict error.
15. **Secure Config (FSA-22):** Secrets bound once; features get abstractions only.
16. **Versioning (FSA-23):** Additive contract growth. Introduce `/v1` first.
17. **Flags (FSA-24):** Feature flags evaluated in host/behavior, boolean passed into request DTO.
18. **Testing Pyramid (FSA-25):** Domain + feature slice + minimal E2E; keep E2E lean.
19. **Additional Entities:** Conform to `application_spec_foodforge.md` domain aggregates and value objects.

## Application Scope Summary

Implement the following aggregates & supporting concepts (see spec for invariants):

- Recipe, Ingredient, MealPlan, ShoppingList, Pantry, RecipeImage, Tag, Rating, RecentHistory.
- Value objects: IngredientQuantity, UnitConversion, TimeRange, ServingYield, RecipeTitle, TagName, MealSlot, ConcurrencyToken, IdempotencyKey.
- Domain events: RecipeCreated, RecipeUpdated, RecipeDeleted, MealPlanCreated, IngredientCreated, ShoppingListGenerated, RatingGiven, RecipeViewed.
- Specifications: ActiveRecipesSpec, RecipesByTagSpec, RecipesSearchSpec, RecentRecipesSpec, MealPlanByDateRangeSpec, ShoppingListForPlanSpec, IngredientsByNamePrefixSpec, PantryItemsForUserSpec.
- Feature catalog (commands/queries) per spec section 8.

All invariants, validation, events, and error taxonomy must match `application_spec_foodforge.md`.

## Step-by-Step Implementation Plan

Please proceed step-by-step. I will review each step.

### Step 1: Solution & Core Toolkit

1. Create solution + projects: `NetForge.Core`, `FoodForge.Domain`, `FoodForge.Features`, `FoodForge.Infrastructure`, `FoodForge.Presentation.Api`, optional `FoodForge.Presentation.Grpc`, optional `FoodForge.AppHost` (Aspire), `FoodForge.Background` (Outbox dispatcher), optional `FoodForge.Clients`.
2. Implement/port NetForge Core primitives (if not already): Result, Error, Mediator, Pipeline behaviors (Validation, UnitOfWork, Logging stub, Timing stub), ForgeValidator, Specification combinators (And/Or/Not/Page/OrderBy), Entity, ValueObject, SmartEnum, DomainEvent base, ForgeMapper (minimal), IUnitOfWork, IRepository&lt;T&gt;, IReadableSpecificationEvaluator.
3. DI: `services.AddNetForgeCore()` registers mediator + behaviors + mapping + validator discovery.

### Step 2: Domain Layer

1. Define aggregates per spec (Recipe, Ingredient, MealPlan, ShoppingList, Pantry, Tag, Rating, RecentHistory, RecipeImage) with invariants enforced in constructors/mutators.
2. Add domain events emission inside aggregates (e.g., `RecipeCreated`, `RecipeUpdated`).
3. Define repository interfaces (generic + specific) supporting spec querying & paging.
4. Define UoW abstraction (CommitAsync, maybe Enlist/Begin for advanced scenarios).
5. Provide shared specifications set.

### Step 3: Feature Slices (Phased)

Implement slices incrementally (each as one file):

1. `RecipesFeature` (CreateRecipe, UpdateRecipe, CopyRecipe, DeleteRecipe, SearchRecipes, GetRecipeDetail, AssignTagsToRecipe, RateRecipe, ListRecentRecipes)
2. `IngredientsFeature` (CreateIngredient, SearchIngredients)
3. `MealPlansFeature` (CreateMealPlan, UpdateMealPlan)
4. `ShoppingListsFeature` (GenerateShoppingList, GetShoppingList)
5. `PantryFeature` (AdjustPantryItem)
6. `TagsFeature` (CreateTag, ListTags)
7. `ImagesFeature` (AddRecipeImage – command; ProcessRecipeImage – background handler)

All slices: nested validators, specs, explicit Results, domain event raising only inside entities.

### Step 4: Infrastructure Layer

1. EF Core DbContext + configurations (row version token, soft delete flags, indexes for search fields, Tag uniqueness).
2. Repositories (EF) + optional read-model Dapper repo for search.
3. Outbox table + repository + background dispatcher (FSA-18).
4. UnitOfWork implementation (transaction + post-commit domain event dispatch + enqueue outbox).
5. Specification evaluator translating expression tree.
6. Caching decorators (optional) for heavy read specs (RecipesSearchSpec facets) (FSA-19).
7. Idempotent image upload store (hash check) (FSA-17).

### Step 5: API Host (Minimal APIs)

1. Add `Program.cs` with: correlation ID middleware, structured logging, `AddNetForgeCore()`, `AddInfrastructureData()`, health checks.
2. Map versioned routes `/v1/...` grouping per aggregate (Recipes, Ingredients, MealPlans, ShoppingLists, Tags, Pantry, Ratings, Images).
3. Implement unified Result -> `IResult` mapping (error taxonomy to status codes; pagination metadata via headers).
4. Ensure all collection endpoints require `page` & `pageSize` query params (bounded) (FSA-20).

### Step 6: Testing Strategy

1. Domain unit tests (invariants, value objects, events raised).
2. Feature slice tests (mediator through pipeline with in-memory repos).
3. API contract tests (minimal subset) verifying status codes + headers + paging.
4. Outbox dispatcher test (stored event -> published stub call).
5. Concurrency conflict test (two updates -> second conflict Result).
6. Pagination boundary tests (pageSize max enforcement).

### Step 7: Background & Optional Extensions

1. Background service for Outbox.
2. Image processing stub (logs transformation request) – idempotent.
3. Optional gRPC surface for selected read endpoints.
4. Aspire AppHost for local orchestration (observability wiring).
5. Optional typed client project demonstrating consumption of the API (FSA-13).

Deliver incrementally: complete Step 1, then Step 2, etc. After each step, output a concise diff summary and any rule IDs satisfied.

---

Cross-Document Navigation: [Architecture Rules](./fused_slice_architecture.md) | [Toolkit](./netforge_core.md) | [Design Doc](./netforge_core_design.md) | [App Spec](./application_spec_foodforge.md) | [Readme](./readme.md)
