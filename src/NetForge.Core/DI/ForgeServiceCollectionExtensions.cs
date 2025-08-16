using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using NetForge.Core.Abstractions;
using NetForge.Core.Options;

namespace NetForge.Core.DI;

public static class ForgeServiceCollectionExtensions
{
    /// <summary>
    /// Registers NetForge core primitives (Mediator, pipeline behaviors, validators, etc.).
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configure">Optional. An action to configure the ForgeOptions.</param>
    public static IServiceCollection AddNetForgeCore(this IServiceCollection services, Action<ForgeOptions>? configure = null)
    {
        services.AddOptions<ForgeOptions>();
        if (configure is not null)
        {
            services.PostConfigure(configure);
        }

        services.TryAddSingleton<IForgeMediator, ForgeMediator>();
        // TODO(core-di-001): Add assembly scanning for handlers, validators, behaviors.
        // TODO(core-di-005): Respect ForgeOptions.EnableAssemblyScanning.
        // TODO(core-di-006): Provide overload to pass assemblies explicitly.

        // Register default behaviors conditionally
        // TODO(core-di-007): Register validation behavior automatically when validators present.

        // Expose options for direct injection
        services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<ForgeOptions>>().Value);
        return services;
    }
}
