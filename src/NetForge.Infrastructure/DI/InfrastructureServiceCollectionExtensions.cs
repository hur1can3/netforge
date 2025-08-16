using Microsoft.Extensions.DependencyInjection;
using NetForge.Core.UnitOfWork;
using NetForge.Infrastructure.Persistence.UnitOfWork;

namespace NetForge.Infrastructure.DI;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddNetForgeInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IForgeUnitOfWork, NoOpUnitOfWork>(); // TODO(infra-di-002): Replace with EF Core UoW.
        return services;
    }
}
