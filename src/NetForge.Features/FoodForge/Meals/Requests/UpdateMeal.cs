using NetForge.Core.Abstractions;
using NetForge.Core.Results;
using NetForge.Core.Validation;
using NetForge.Core.Repositories;
using NetForge.Core.UnitOfWork;
using NetForge.Features.FoodForge.Meals.Entities;

namespace NetForge.Features.FoodForge.Meals.Requests;

public sealed record UpdateMeal(Guid Id, string Name, int Calories) : ForgeRequest<ForgeResult>
{
    public sealed class Validator : ForgeValidator<UpdateMeal>
    {
        protected override void OnValidate(UpdateMeal instance)
        {
            RuleFor(instance.Id == Guid.Empty, ForgeError.Validation("Id", "Id required"));
            RuleFor(string.IsNullOrWhiteSpace(instance.Name), ForgeError.Validation("Name", "Name required"));
            RuleFor(instance.Calories <= 0, ForgeError.Validation("Calories", "Calories must be positive"));
        }
    }

    public sealed class Handler : ForgeRequestHandler<UpdateMeal, ForgeResult>
    {
        private readonly IForgeRepository<Meal, Guid> _repo;
        private readonly IForgeUnitOfWork _uow;
        public Handler(IForgeRepository<Meal, Guid> repo, IForgeUnitOfWork uow)
        { _repo = repo; _uow = uow; }

        public override async Task<ForgeResult> Handle(UpdateMeal request, CancellationToken cancellationToken)
        {
            var existing = await _repo.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
            if (existing is null)
            {
                return ForgeResults.Failure(ForgeError.NotFound("Meal", "Meal not found")); // TODO(foodforge-app-008): Localize message / code constants.
            }
            existing.Update(request.Name, request.Calories);
            // TODO(foodforge-app-006): Persist update if needed for chosen ORM.
            await _uow.CommitAsync(cancellationToken).ConfigureAwait(false); // TODO(foodforge-app-007): Move commit to pipeline behavior later.
            return ForgeResults.Success();
        }
    }
}
