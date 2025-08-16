using System.Collections.Generic;
using NetForge.Core.Abstractions;
using NetForge.Core.Results;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace NetForge.Tests.Core;

public class ForgeMediatorResultPipelineTests
{
    private sealed record SuccessRequest(string Msg) : ForgeRequest<ForgeResult<string>>;
    private sealed record FailureRequest() : ForgeRequest<ForgeResult<string>>;
    private sealed record ThrowingResultRequest() : ForgeRequest<ForgeResult<string>>;

    private sealed class Collector : List<string> { }

    private sealed class SuccessHandler : ForgeRequestHandler<SuccessRequest, ForgeResult<string>>
    {
        private readonly Collector _c; public SuccessHandler(Collector c)=>_c=c;
        public override Task<ForgeResult<string>> Handle(SuccessRequest request, CancellationToken cancellationToken)
        {
            _c.Add("handler");
            return Task.FromResult(ForgeResults.Success(request.Msg));
        }
    }

    private sealed class FailureHandler : ForgeRequestHandler<FailureRequest, ForgeResult<string>>
    {
        private readonly Collector _c; public FailureHandler(Collector c)=>_c=c;
        public override Task<ForgeResult<string>> Handle(FailureRequest request, CancellationToken cancellationToken)
        {
            _c.Add("handler");
            return Task.FromResult(ForgeResults.Failure<string>(ForgeError.Validation("Field","bad")));
        }
    }

    private sealed class ThrowingResultHandler : ForgeRequestHandler<ThrowingResultRequest, ForgeResult<string>>
    {
        public override Task<ForgeResult<string>> Handle(ThrowingResultRequest request, CancellationToken cancellationToken)
            => throw new InvalidOperationException("explode");
    }

    // Behaviors
    private sealed class ResultBehavior : ForgePipelineBehavior<SuccessRequest, ForgeResult<string>>
    {
        private readonly Collector _c; public ResultBehavior(Collector c)=>_c=c;
        public override async Task<ForgeResult<string>> Handle(SuccessRequest request, ForgeRequestHandlerExecution<ForgeResult<string>> nextHandler, CancellationToken ct)
        {
            _c.Add("behavior-before");
            var r = await nextHandler();
            _c.Add("behavior-after");
            return r;
        }
    }
    private sealed class FailureBehavior : ForgePipelineBehavior<FailureRequest, ForgeResult<string>>
    {
        private readonly Collector _c; public FailureBehavior(Collector c)=>_c=c;
        public override async Task<ForgeResult<string>> Handle(FailureRequest request, ForgeRequestHandlerExecution<ForgeResult<string>> nextHandler, CancellationToken ct)
        {
            _c.Add("behavior-before");
            var r = await nextHandler();
            _c.Add("behavior-after");
            return r;
        }
    }

    // Unified pre/post for multiple request types via open generic registration unsupported here; declare per type.
    private sealed class SuccessPre : IForgePreProcessor<SuccessRequest, ForgeResult<string>>
    { private readonly Collector _c; public SuccessPre(Collector c)=>_c=c; public Task PreProcess(SuccessRequest r, CancellationToken ct){_c.Add("pre");return Task.CompletedTask;} }
    private sealed class SuccessPost : IForgePostProcessor<SuccessRequest, ForgeResult<string>>
    { private readonly Collector _c; public SuccessPost(Collector c)=>_c=c; public Task PostProcess(SuccessRequest r, ForgeResult<string> resp, CancellationToken ct){_c.Add("post");return Task.CompletedTask;} }

    private sealed class FailurePre : IForgePreProcessor<FailureRequest, ForgeResult<string>>
    { private readonly Collector _c; public FailurePre(Collector c)=>_c=c; public Task PreProcess(FailureRequest r, CancellationToken ct){_c.Add("pre");return Task.CompletedTask;} }
    private sealed class FailurePost : IForgePostProcessor<FailureRequest, ForgeResult<string>>
    { private readonly Collector _c; public FailurePost(Collector c)=>_c=c; public Task PostProcess(FailureRequest r, ForgeResult<string> resp, CancellationToken ct){_c.Add("post");return Task.CompletedTask;} }

    private sealed class ThrowingPre : IForgePreProcessor<ThrowingResultRequest, ForgeResult<string>>
    { public Task PreProcess(ThrowingResultRequest r, CancellationToken ct)=>Task.CompletedTask; }
    private sealed class ThrowingPost : IForgePostProcessor<ThrowingResultRequest, ForgeResult<string>>
    { public Task PostProcess(ThrowingResultRequest r, ForgeResult<string> resp, CancellationToken ct)=>Task.CompletedTask; }

    private sealed class ThrowingExceptionHandler : IForgeExceptionHandler<ThrowingResultRequest, ForgeResult<string>>
    {
        public Task<ForgeExceptionHandlingResult<ForgeResult<string>>> HandleException(ThrowingResultRequest request, Exception exception, CancellationToken ct)
        {
            if (exception is InvalidOperationException)
            {
                return Task.FromResult(ForgeExceptionHandlingResult<ForgeResult<string>>.HandledResult(ForgeResults.Failure<string>(ForgeError.Unexpected("mapped"))));
            }
            return Task.FromResult(ForgeExceptionHandlingResult<ForgeResult<string>>.NotHandled());
        }
    }

    [Fact]
    public async Task Result_Success_Full_Order()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IForgeMediator, ForgeMediator>();
        services.AddSingleton<Collector>();
        services.AddSingleton<ForgeRequestHandler<SuccessRequest, ForgeResult<string>>, SuccessHandler>();
        services.AddSingleton<ForgePipelineBehavior<SuccessRequest, ForgeResult<string>>, ResultBehavior>();
        services.AddSingleton<IForgePreProcessor<SuccessRequest, ForgeResult<string>>, SuccessPre>();
        services.AddSingleton<IForgePostProcessor<SuccessRequest, ForgeResult<string>>, SuccessPost>();
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IForgeMediator>();
        var collector = sp.GetRequiredService<Collector>();
        var result = await mediator.Send(new SuccessRequest("MSG"));
        Assert.True(result.IsSuccess);
        Assert.Equal("MSG", result.Value);
        Assert.Equal(new[]{"pre","behavior-before","handler","behavior-after","post"}, collector.ToArray());
    }

    [Fact]
    public async Task Result_Failure_Still_Runs_Post()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IForgeMediator, ForgeMediator>();
        services.AddSingleton<Collector>();
        services.AddSingleton<ForgeRequestHandler<FailureRequest, ForgeResult<string>>, FailureHandler>();
        services.AddSingleton<ForgePipelineBehavior<FailureRequest, ForgeResult<string>>, FailureBehavior>();
        services.AddSingleton<IForgePreProcessor<FailureRequest, ForgeResult<string>>, FailurePre>();
        services.AddSingleton<IForgePostProcessor<FailureRequest, ForgeResult<string>>, FailurePost>();
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IForgeMediator>();
        var collector = sp.GetRequiredService<Collector>();
        var result = await mediator.Send(new FailureRequest());
        Assert.True(result.IsFailure);
        Assert.Single(result.Errors);
        Assert.Equal(new[]{"pre","behavior-before","handler","behavior-after","post"}, collector.ToArray());
    }

    [Fact]
    public async Task Result_Exception_Handled_No_PostProcessors()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IForgeMediator, ForgeMediator>();
        services.AddSingleton<ForgeRequestHandler<ThrowingResultRequest, ForgeResult<string>>, ThrowingResultHandler>();
        services.AddSingleton<IForgePreProcessor<ThrowingResultRequest, ForgeResult<string>>, ThrowingPre>();
        services.AddSingleton<IForgePostProcessor<ThrowingResultRequest, ForgeResult<string>>, ThrowingPost>();
        services.AddSingleton<IForgeExceptionHandler<ThrowingResultRequest, ForgeResult<string>>, ThrowingExceptionHandler>();
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IForgeMediator>();
        var result = await mediator.Send(new ThrowingResultRequest());
        Assert.True(result.IsFailure);
        Assert.Single(result.Errors);
        Assert.Equal("Unexpected", result.Errors[0].Code);
    }
}
