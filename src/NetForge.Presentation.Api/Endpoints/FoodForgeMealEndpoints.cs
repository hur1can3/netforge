using NetForge.Core.Abstractions;
using NetForge.Core.Results;
using NetForge.Features.FoodForge.Meals.Requests;

namespace NetForge.Presentation.Api.Endpoints;

internal static class FoodForgeMealEndpoints // changed to internal per CA1515 suggestion
{
    // TODO(foodforge-api-001): Add error mapping strategy (Result -> IResult) with ProblemDetails.
    public static IEndpointRouteBuilder MapFoodForgeMealEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/foodforge/meals");

        group.MapPost("", async (CreateMeal cmd, IForgeMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(cmd, ct).ConfigureAwait(false);
            return result.IsSuccess ? Results.Created($"/api/foodforge/meals/{result.Value}", new { id = result.Value }) : MapErrors(result.Errors);
        });

        group.MapGet("{id:guid}", async (Guid id, IForgeMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetMeal(id), ct).ConfigureAwait(false);
            return result.IsSuccess ? Results.Ok(result.Value) : MapErrors(result.Errors);
        });

        group.MapPut("{id:guid}", async (Guid id, UpdateMeal update, IForgeMediator mediator, CancellationToken ct) =>
        {
            var cmd = update with { Id = id };
            var result = await mediator.Send(cmd, ct).ConfigureAwait(false);
            return result.IsSuccess ? Results.NoContent() : MapErrors(result.Errors);
        });

        return app;
    }

    private static IResult MapErrors(IReadOnlyList<ForgeError> errors)
    {
        // TODO(foodforge-api-002): Replace with structured ProblemDetails mapping.
        return Results.BadRequest(new { errors = errors.Select(e => new { e.Code, e.Message }) });
    }
}
