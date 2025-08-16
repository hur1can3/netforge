using NetForge.Domain.Entities;
using NetForge.Core.DomainEvents; // For future domain events

namespace NetForge.Features.FoodForge.Meals.Entities;

public sealed class Meal : ForgeEntity<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public int Calories { get; private set; }

    private Meal() { } // EF / serialization

    private Meal(string name, int calories)
    {
        Id = Guid.NewGuid(); // TODO(foodforge-domain-004): Replace with ULID or KSUID if required.
        Name = name;
        Calories = calories;
        // TODO(foodforge-domain-001): Raise MealCreated domain event.
    }

    public static Meal Create(string name, int calories)
    {
        // TODO(foodforge-domain-002): Add guard + validation integration.
        return new Meal(name, calories);
    }

    public void Update(string name, int calories)
    {
        Name = name;
        Calories = calories;
        // TODO(foodforge-domain-003): Raise MealUpdated domain event.
    }
}
