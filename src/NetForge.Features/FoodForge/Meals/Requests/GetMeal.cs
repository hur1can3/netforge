using NetForge.Core.Abstractions;
using NetForge.Core.Results;
using NetForge.Core.Repositories;
using NetForge.Features.FoodForge.Meals.Entities;

namespace NetForge.Features.FoodForge.Meals.Requests;

public sealed record MealDto(Guid Id, string Name, int Calories);

public sealed record GetMeal(Guid Id) : ForgeRequest<ForgeResult<MealDto>>
{
    public sealed class Handler : ForgeRequestHandler<GetMeal, ForgeResult<MealDto>>
    {
        private readonly IForgeRepository<Meal, Guid> _repo;
        public Handler(IForgeRepository<Meal, Guid> repo) => _repo = repo; // TODO(foodforge-app-009): Add guard.

        public override async Task<ForgeResult<MealDto>> Handle(GetMeal request, CancellationToken cancellationToken)
        {
            var meal = await _repo.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
            if (meal is null) return ForgeResults.Failure<MealDto>(ForgeError.NotFound("Meal", "Meal not found"));
            var dto = new MealDto(meal.Id, meal.Name, meal.Calories);
            return ForgeResults.Success(dto);
        }
    }
}
