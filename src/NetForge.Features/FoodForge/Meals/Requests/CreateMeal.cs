using NetForge.Core.Abstractions;
using NetForge.Core.Results;
using NetForge.Core.Validation;
using NetForge.Features.FoodForge.Meals.Entities;
using NetForge.Core.Repositories;
using NetForge.Core.UnitOfWork;

namespace NetForge.Features.FoodForge.Meals.Requests;

public sealed record CreateMeal(string Name, int Calories) : ForgeRequest<ForgeResult<Guid>>
{
    public sealed class Validator : ForgeValidator<CreateMeal>
    {
        protected override void OnValidate(CreateMeal instance)
        {
            RuleFor(string.IsNullOrWhiteSpace(instance.Name), ForgeError.Validation("Name", "Name required"));
            RuleFor(instance.Calories <= 0, ForgeError.Validation("Calories", "Calories must be positive"));
        }
    }

    public sealed class Handler : ForgeRequestHandler<CreateMeal, ForgeResult<Guid>>
    {
        private readonly IForgeRepository<Meal, Guid> _repo;
        private readonly IForgeUnitOfWork _uow;
        public Handler(IForgeRepository<Meal, Guid> repo, IForgeUnitOfWork uow)
        {
            _repo = repo; // TODO(foodforge-app-001): Add guard checks.
            _uow = uow;
        }
        public override async Task<ForgeResult<Guid>> Handle(CreateMeal request, CancellationToken cancellationToken)
        {
            var meal = Meal.Create(request.Name, request.Calories);
            await _repo.AddAsync(meal, cancellationToken).ConfigureAwait(false); // TODO(foodforge-app-002): Add validation for duplicates.
            await _uow.CommitAsync(cancellationToken).ConfigureAwait(false); // TODO(foodforge-app-007): Move commit to pipeline behavior later.
            return ForgeResults.Success(meal.Id);
        }
    }
}
