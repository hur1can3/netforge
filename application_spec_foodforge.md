# Sample Application Specification: FoodForge (Inspired by FoodStuffs)

Version: 0.1 (Specification Only)

This document specifies the domain model, ubiquitous language, feature set, and business rules for a sample application built with the Fused Slice Architecture (FSA) and NetForge Core toolkit. It draws inspiration from the public FoodStuffs project while remaining implementation-agnostic.

---

## 1. Vision & Domain Summary

FoodForge helps users curate, organize, discover, and execute meal preparation via Recipes, Ingredients, Meal Plans, and Shopping Lists with tagging, categorization, and search/discovery workflows.

Primary goals:

- Reduce friction in planning weekly meals.
- Ensure ingredient reuse and accurate shopping list aggregation.
- Promote recipe discovery (randomization, search facets, copy/adaptation flows).
- Preserve user trust via data integrity (no silent overwrites), performance (bounded results), and reliability (idempotent side-effects for external image processing / notifications).

---

## 2. Bounded Contexts (Single Application Context for Now)

All functionality resides in a single bounded context for v0.1. Potential future splits:

- Catalog (public recipe repository)
- Personalization (user meal planning and history)
- Commerce (if integrated ordering emerges)

---

## 3. Ubiquitous Language

| Term | Definition |
|------|------------|
| Recipe | User-authored structured set of steps + ingredients with metadata. |
| Ingredient | A fundamental item with name, optional brand, unit normalization metadata. |
| Ingredient Line | A quantified reference to an Ingredient within a Recipe (qty + unit + free-form note). |
| Tag | A categorical label (e.g., Vegan, Dessert, Quick) used for filtering/faceting. |
| Meal Plan | A weekly (or custom span) association of Recipes to calendar days and optional meal slot. |
| Meal Slot | Category within a day (Breakfast, Lunch, Dinner, Snack). |
| Shopping List | Aggregated ingredient requirements for a selected set of Recipes / Meal Plan. |
| Unit | Measurement (grams, ml, tsp, tbsp, cup, unit) convertible within category. |
| User Pantry Item | User-specific current quantity on hand for an Ingredient. |
| Recipe Image | Uploaded visual asset (original + processed variants) with transformation metadata. |
| Copy of Recipe | A derived Recipe referencing an origin RecipeId for lineage. |
| Search Facet | Aggregated dimension value for filtering (Tag, MealSlot, PrepTimeRange). |
| Prep Time | Declared estimated time to prepare a recipe. |
| Yield | Number of servings produced. |
| Rating | User rating (1–5) associated with a Recipe (optional). |
| Recent History Entry | Entry in a user-specific list of recently viewed Recipes. |

---

## 4. Aggregate Roots & Entities

| Aggregate | Purpose | Invariants (High-Level) |
|-----------|---------|------------------------|
| Recipe | Authoring + discovery unit | Title required; >=1 step; >=1 ingredient line; each line references active Ingredient; yield > 0; prep time >= 0; unique (UserId, Title) per owner. |
| Ingredient | Canonical ingredient metadata | Name required; unique (Name, Form) per tenant/global; default unit category set. |
| MealPlan | Weekly or custom plan | Date range <= 8 weeks; entries reference existing Recipes; no duplicate (Date, MealSlot, RecipeId) per plan. |
| ShoppingList | Aggregated list from Recipes/Plan | Derived lines aggregated by Ingredient + normalized unit; quantity > 0. |
| Pantry | User on-hand stock | One entry per (UserId, IngredientId); quantity >= 0. |
| RecipeImage | Managed asset | Linked to existing Recipe; at most 1 primary image flagged. |
| Tag | Classification | Name required; unique name (case-insensitive). |
| Rating | User sentiment | Value 1–5; one rating per (UserId, RecipeId). |
| RecentHistory | Recency tracking | Ordered by last viewed timestamp; capped to N (config). |

---

## 5. Value Objects

| Value Object | Components | Notes |
|--------------|-----------|-------|
| IngredientQuantity | Amount (decimal), Unit (UnitCode) | Converts to base unit for aggregation. |
| UnitConversion | SourceUnit, TargetUnit, Factor | Must be > 0. |
| TimeRange | Minutes (int) | Never negative. |
| ServingYield | Servings (int) | > 0. |
| RecipeTitle | string | Trimmed, length 3–140, normalized for uniqueness. |
| TagName | string | Case-insensitive value semantics. |
| MealSlot | enum (Breakfast,Lunch,Dinner,Snack,Other) | Smart enum for extensibility. |
| ConcurrencyToken | string/rowversion | Changed on each mutation (FSA-21). |
| IdempotencyKey | string | Provided for external side-effects (image processing). |

---

## 6. Domain Events

| Event | Trigger | Effect |
|-------|---------|--------|
| RecipeCreated | New recipe persisted | Queue image processing (if image) via outbox (FSA-18). |
| RecipeUpdated | Edit saved | Invalidate recipe search index entry (FSA-19 caching invalidation) + reindex. |
| RecipeDeleted | Deleted | Remove from search index, purge images. |
| MealPlanCreated | New plan | Potential notification (future). |
| IngredientCreated | New ingredient | Recalculate dependent ShoppingLists (future). |
| ShoppingListGenerated | Generated from plan | Email/export generation job. |
| RatingGiven | Rating added/updated | Update aggregate rating & facet stats. |
| RecipeViewed | Displayed to user | Append to RecentHistory (bounded N). |

All domain events published only post-commit (FSA-07).

---

## 7. Specifications (Query Modeling)

Key reusable specifications (composable per FSA-08):

- ActiveRecipesSpec (filters out soft-deleted / drafts)
- RecipesByTagSpec(tagIds[])
- RecipesSearchSpec(text, tags[], timeRange, mealSlots[], minRating)
- RecentRecipesSpec(userId, limit)
- MealPlanByDateRangeSpec(userId, start, end)
- ShoppingListForPlanSpec(planId)
- IngredientsByNamePrefixSpec(prefix)
- PantryItemsForUserSpec(userId)

All collection results must be paginated (FSA-20); specs expose Page(page,size).

---

## 8. Core Use Cases (Feature Catalog)

| Feature | Summary | Primary Rules / Constraints | Result Types |
|---------|---------|-----------------------------|--------------|
| CreateRecipe | Author new recipe with metadata & lines | Enforces Recipe invariants; validates ingredient references; optional image stub | Result&lt;Guid&gt; |
| UpdateRecipe | Modify title, steps, lines, tags | Concurrency token check (FSA-21); reindex event | Result |
| CopyRecipe | Clone recipe for adaptation | References originId; resets ratings/history | Result&lt;Guid&gt; |
| DeleteRecipe | Soft delete or hard delete if no dependencies | Prevent delete if referenced by MealPlan unless forced | Result |
| AddRecipeImage | Upload + queue process | Idempotent via (RecipeId, FileHash) (FSA-17) | Result&lt;ImageId&gt; |
| ProcessRecipeImage | Background scaling/cropping | Outbox triggered; ensures idempotency | Result |
| SearchRecipes | Faceted search with pagination | Uses composite spec; bounded page size (FSA-20) | Result&lt;Paged&lt;RecipeSummary&gt;&gt; |
| GetRecipeDetail | Retrieve recipe with ingredients & tags | Includes aggregated rating | Result&lt;RecipeDetail&gt; |
| ListRecentRecipes | Recent history | Limit N; fallback random if empty | Result&lt;IReadOnlyList&lt;RecipeSummary&gt;&gt; |
| RateRecipe | Add/update user rating | One per user; updates aggregates | Result |
| CreateMealPlan | Create dated plan with recipes | Range <= 8 weeks; entries valid | Result&lt;Guid&gt; |
| UpdateMealPlan | Adjust entries | Replace semantics; validates duplicates | Result |
| GenerateShoppingList | Build aggregated list from meal plan | Aggregates & unit normalizes; excludes pantry stock | Result&lt;Guid&gt; |
| GetShoppingList | View aggregated items | Sorted by category/name | Result&lt;ShoppingListDto&gt; |
| AdjustPantryItem | Update on-hand quantity | Never below zero | Result |
| SuggestRecipes | Random/infinite scroll suggestions | Combines random + recent popularity weighting | Result&lt;Paged&lt;RecipeSummary&gt;&gt; |
| ListTags | List all tags | Sorted name ascending | Result&lt;IReadOnlyList&lt;TagDto&gt;&gt; |
| CreateTag | Add new tag | Unique name constraint | Result&lt;Guid&gt; |
| AssignTagsToRecipe | Modify tag associations | Only existing tags; no duplicates | Result |

All features: single fused file (FSA-01), validation via nested validators (FSA-11), repository abstractions only (FSA-03), explicit Result returns (FSA-04), no SaveChanges calls (FSA-06/FSA-12), post-commit events (FSA-07), specs (FSA-08), structured logging (FSA-16), pagination (FSA-20), concurrency tokens (FSA-21), secure config (FSA-22 if needed), idempotency for external side-effects (FSA-17), optional caching decorators for search (FSA-19).

---
## 9. Derived Data & Caching

| Data | Source | Strategy |
|------|--------|----------|
| RecipeSearchIndex | Recipes + tags | Updated via RecipeCreated/Updated/Deleted events (outbox) |
| Facet Aggregates | Index stats | Derived per search query or cached per tag set for short TTL |
| ShoppingList Lines | MealPlan + Pantry | Recomputed on demand (no stale storage) |
| Rating Aggregate | Ratings | Lazy updated on RatingGiven event |

Cache applied via decorators (FSA-19); invalidation driven by domain events.

---
## 10. Security & Authorization (Conceptual)

- Users can only mutate their own Recipes, MealPlans, Pantry, Ratings.
- Tags may be global (admin-managed) or user-specific (future).
- Authorization enforced in host layer (mapping to a userId claim) before mediator send (FSA-15 protocol agnostic features; features receive UserId in command).

---
## 11. Validation Rules (Selected)

| Context | Rule |
|---------|------|
| RecipeTitle | 3–140 chars; trimmed; no duplicates per user. |
| IngredientQuantity | Amount > 0; unit convertible within category. |
| MealPlan | Start <= End; (End - Start) <= 56 days. |
| MealPlanEntry | Recipe exists & not deleted; unique per (Date, Slot, RecipeId). |
| ShoppingListGeneration | Plan exists; Plan not empty. |
| Rating | 1–5 integer. |
| Pagination | PageSize <= 100. |

---
## 12. Error Taxonomy (Explicit Results)

| Category | Example Error Code | Description |
|----------|--------------------|-------------|
| Validation | recipe.title.invalid | Title length or uniqueness violation. |
| Validation | recipe.line.ingredient.missing | Ingredient reference not found. |
| Conflict | recipe.concurrency.conflict | Version mismatch on update. |
| Conflict | mealplan.entry.duplicate | Duplicate slot entry on plan update. |
| NotFound | recipe.not_found | Recipe id does not exist or deleted. |
| NotFound | mealplan.not_found | MealPlan id not found. |
| NotFound | ingredient.not_found | Ingredient id missing. |
| Business | recipe.delete.blocked | Deletion blocked by existing MealPlan. |
| Business | image.duplicate | Duplicate image upload hash. |
| Business | rating.already.exists | User has already rated (on create). |

---

## 13. Observability

- Structured log properties: feature_name, request_id, user_id, correlation_id, success, duration_ms, error_codes[].
- Activity spans: mediator.pipeline, repository.call, outbox.dispatch, image.process.
- Metrics: recipe_search_latency_ms (histogram), mealplan_generate_duration_ms, outbox_pending_events, cache_hit_ratio_search.

---

## 14. Non-Functional Requirements

| Aspect | Target |
|--------|--------|
| P99 Recipe Search | < 800ms (cached facets < 300ms) |
| P99 Generate Shopping List | < 1200ms |
| Max Page Size | 100 |
| Recent History Size | 50 (configurable) |
| Outbox Dispatch Retry Backoff | Exponential, max 6 attempts |

---

## 15. Future Extensions

- Public sharing links with visibility controls.
- Nutrition calculation integration.
- Multi-user collaborative meal plans.
- Subscription-based premium features (advanced analytics, bulk import).

---

## 16. Traceability to FSA Rules

| FSA Rule | Coverage Reference |
|----------|-------------------|
| FSA-01 | Feature catalog fused file requirement |
| FSA-04 | Error taxonomy & Result mapping |
| FSA-06/12 | No direct commits in features |
| FSA-07 | Post-commit events list |
| FSA-08 | Specs section |
| FSA-16 | Observability section |
| FSA-17 | Idempotent image upload/process |
| FSA-18 | Outbox for search index & image events |
| FSA-19 | Caching decorators |
| FSA-20 | Pagination rules |
| FSA-21 | Concurrency token usage |
| FSA-22 | Secure config mention (auth context) |
| FSA-25 | Testing pyramid implied (slice/domain) |

End of specification.
