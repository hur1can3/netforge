using NetForge.Core.DI;
using NetForge.Presentation.Api.Endpoints;
using NetForge.Infrastructure.DI;
using NetForge.Core.Repositories;
using NetForge.Features.FoodForge.Meals.Entities;
using NetForge.Features.FoodForge.Meals.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Core
builder.Services.AddNetForgeCore(); // TODO(foodforge-api-003): Configure options when available.
// Infrastructure
builder.Services.AddNetForgeInfrastructure();
// Feature-specific repositories
builder.Services.AddSingleton<IForgeRepository<Meal, Guid>, InMemoryMealRepository>();

var app = builder.Build();

app.MapGet("/", () => "NetForge API");
app.MapFoodForgeMealEndpoints();

app.Run();
