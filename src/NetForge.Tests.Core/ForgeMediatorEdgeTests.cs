using NetForge.Core.Abstractions;
using NetForge.Core.Results;
using NetForge.Core.Validation;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace NetForge.Tests.Core;

public class ForgeMediatorEdgeTests
{
    private sealed record NoHandlerRequest : ForgeRequest<string>;

    [Fact]
    public async Task MissingHandlerThrows()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IForgeMediator, ForgeMediator>();
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IForgeMediator>();
        await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.Send(new NoHandlerRequest()));
    }

    private sealed record NonResultValidated(string Name) : ForgeRequest<string>
    {
        public sealed class Validator : ForgeValidator<NonResultValidated>
        {
            protected override void OnValidate(NonResultValidated instance)
            { RuleFor(string.IsNullOrWhiteSpace(instance.Name), ForgeError.Validation("Name","Name required")); }
        }
    }
    private sealed class NonResultHandler : ForgeRequestHandler<NonResultValidated,string>
    { public override Task<string> Handle(NonResultValidated request, CancellationToken cancellationToken)=>Task.FromResult(request.Name); }

    [Fact]
    public async Task ValidationBehavior_With_NonResult_Response_Throws()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IForgeMediator, ForgeMediator>();
        services.AddSingleton<ForgeRequestHandler<NonResultValidated,string>, NonResultHandler>();
        services.AddSingleton<ForgeValidator<NonResultValidated>, NonResultValidated.Validator>();
        services.AddSingleton(typeof(ForgePipelineBehavior<,>), typeof(ForgeValidationBehavior<,>));
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IForgeMediator>();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.Send(new NonResultValidated("")));
        Assert.Contains("Validation behavior", ex.Message);
    }
}
