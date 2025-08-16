using NetForge.Core.Repositories;
using NetForge.Features.FoodForge.Meals.Entities;

namespace NetForge.Features.FoodForge.Meals.Repositories;

public sealed class InMemoryMealRepository : IForgeRepository<Meal, Guid>
{
    private static readonly Dictionary<Guid, Meal> Store = new(); // TODO(foodforge-repo-001): Replace with persistent storage.

    public Task AddAsync(Meal entity, CancellationToken ct = default)
    {
        if (entity.Id == Guid.Empty)
        {
            typeof(Meal).GetProperty("Id")!.SetValue(entity, Guid.NewGuid()); // TODO(foodforge-repo-002): Replace with proper ID generation strategy.
        }
        Store[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public Task<Meal?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        Store.TryGetValue(id, out var meal);
        return Task.FromResult(meal);
    }
}
