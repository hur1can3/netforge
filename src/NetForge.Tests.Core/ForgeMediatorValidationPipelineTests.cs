using NetForge.Core.Abstractions;
using NetForge.Core.Results;
using NetForge.Core.Validation;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace NetForge.Tests.Core;

public class ForgeMediatorValidationPipelineTests
{
    private sealed record ValidatedSuccess(string Name) : ForgeRequest<ForgeResult<string>>
    {
        public sealed class Validator : ForgeValidator<ValidatedSuccess>
        {
            protected override void OnValidate(ValidatedSuccess instance)
            {
                RuleFor(string.IsNullOrWhiteSpace(instance.Name), ForgeError.Validation("Name", "Name required"));
            }
        }
    }

    private sealed record ValidatedValue(string Name) : ForgeRequest<ForgeResult<string>>
    {
        public sealed class Validator : ForgeValidator<ValidatedValue>
        {
            protected override void OnValidate(ValidatedValue instance)
            {
                RuleFor(string.IsNullOrWhiteSpace(instance.Name), ForgeError.Validation("Name", "Name required"));
            }
        }
    }

    private sealed class SuccessHandler : ForgeRequestHandler<ValidatedSuccess, ForgeResult<string>>
    {
        public override Task<ForgeResult<string>> Handle(ValidatedSuccess request, CancellationToken cancellationToken)
            => Task.FromResult(ForgeResults.Success(request.Name + "-HANDLED"));
    }

    private sealed class ValueHandler : ForgeRequestHandler<ValidatedValue, ForgeResult<string>>
    {
        public override Task<ForgeResult<string>> Handle(ValidatedValue request, CancellationToken cancellationToken)
            => Task.FromResult(ForgeResults.Success(request.Name + "-VAL"));
    }

    [Fact]
    public async Task Validation_Success_Allows_Handler()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IForgeMediator, ForgeMediator>();
        services.AddSingleton<ForgeRequestHandler<ValidatedSuccess, ForgeResult<string>>, SuccessHandler>();
        services.AddSingleton<ForgeValidator<ValidatedSuccess>, ValidatedSuccess.Validator>();
        services.AddSingleton(typeof(ForgePipelineBehavior<,>), typeof(ForgeValidationBehavior<,>));
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IForgeMediator>();

        var result = await mediator.Send(new ValidatedSuccess("Ok"));
        Assert.True(result.IsSuccess);
        Assert.Equal("Ok-HANDLED", result.Value);
    }

    [Fact]
    public async Task Validation_Failure_ShortCircuits_Handler()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IForgeMediator, ForgeMediator>();
        services.AddSingleton<ForgeRequestHandler<ValidatedValue, ForgeResult<string>>, ValueHandler>();
        services.AddSingleton<ForgeValidator<ValidatedValue>, ValidatedValue.Validator>();
        services.AddSingleton(typeof(ForgePipelineBehavior<,>), typeof(ForgeValidationBehavior<,>));
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IForgeMediator>();

        var result = await mediator.Send(new ValidatedValue(""));
        Assert.True(result.IsFailure);
        Assert.Single(result.Errors);
        Assert.Equal("Validation:Name", result.Errors[0].Code);
    }
}
