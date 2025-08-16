using System.Collections.Generic;
using NetForge.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace NetForge.Tests.Core;

public class ForgeMediatorPipelineTests
{
    // --- Test Request Types ---
    private sealed record PipelineRequest : ForgeRequest<string>;
    private sealed record FailingRequest : ForgeRequest<string>;

    // --- Shared collector service ---
    private sealed class Collector : List<string> { }

    // --- Handler Implementations ---
    private sealed class PipelineHandler : ForgeRequestHandler<PipelineRequest, string>
    {
        private readonly Collector _collector;
        public PipelineHandler(Collector collector) => _collector = collector;
        public override Task<string> Handle(PipelineRequest request, CancellationToken cancellationToken)
        {
            _collector.Add("handler");
            return Task.FromResult("OK");
        }
    }

    private sealed class FailingHandler : ForgeRequestHandler<FailingRequest, string>
    {
        public override Task<string> Handle(FailingRequest request, CancellationToken cancellationToken)
            => throw new InvalidOperationException("boom");
    }

    // --- Pre Processors ---
    private sealed class Pre1 : IForgePreProcessor<PipelineRequest, string>
    {
        private readonly Collector _collector; public Pre1(Collector c) => _collector = c;
        public Task PreProcess(PipelineRequest request, CancellationToken ct)
        { _collector.Add("pre1"); return Task.CompletedTask; }
    }
    private sealed class Pre2 : IForgePreProcessor<PipelineRequest, string>
    {
        private readonly Collector _collector; public Pre2(Collector c) => _collector = c;
        public Task PreProcess(PipelineRequest request, CancellationToken ct)
        { _collector.Add("pre2"); return Task.CompletedTask; }
    }

    // --- Post Processors ---
    private sealed class Post1 : IForgePostProcessor<PipelineRequest, string>
    {
        private readonly Collector _collector; public Post1(Collector c) => _collector = c;
        public Task PostProcess(PipelineRequest request, string response, CancellationToken ct)
        { _collector.Add("post1"); return Task.CompletedTask; }
    }
    private sealed class Post2 : IForgePostProcessor<PipelineRequest, string>
    {
        private readonly Collector _collector; public Post2(Collector c) => _collector = c;
        public Task PostProcess(PipelineRequest request, string response, CancellationToken ct)
        { _collector.Add("post2"); return Task.CompletedTask; }
    }

    // --- Behaviors ---
    private sealed class BehaviorA : ForgePipelineBehavior<PipelineRequest, string>
    {
        private readonly Collector _collector; public BehaviorA(Collector c) => _collector = c;
        public override async Task<string> Handle(PipelineRequest request, ForgeRequestHandlerExecution<string> nextHandler, CancellationToken ct)
        {
            _collector.Add("behaviorA-before");
            var result = await nextHandler();
            _collector.Add("behaviorA-after");
            return result;
        }
    }
    private sealed class BehaviorB : ForgePipelineBehavior<PipelineRequest, string>
    {
        private readonly Collector _collector; public BehaviorB(Collector c) => _collector = c;
        public override async Task<string> Handle(PipelineRequest request, ForgeRequestHandlerExecution<string> nextHandler, CancellationToken ct)
        {
            _collector.Add("behaviorB-before");
            var result = await nextHandler();
            _collector.Add("behaviorB-after");
            return result;
        }
    }

    // --- Exception Handler ---
    private sealed class FailingRequestExceptionHandler : IForgeExceptionHandler<FailingRequest, string>
    {
        public Task<ForgeExceptionHandlingResult<string>> HandleException(FailingRequest request, Exception exception, CancellationToken ct)
        {
            if (exception is InvalidOperationException)
            {
                return Task.FromResult(ForgeExceptionHandlingResult<string>.HandledResult("RECOVERED"));
            }
            return Task.FromResult(ForgeExceptionHandlingResult<string>.NotHandled());
        }
    }

    [Fact]
    public async Task Pipeline_Orders_Pre_Behaviors_Handler_Post()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IForgeMediator, ForgeMediator>();
        services.AddSingleton<Collector>();

        services.AddSingleton<ForgeRequestHandler<PipelineRequest, string>, PipelineHandler>();

        // Register in expected order A then B
        services.AddSingleton<ForgePipelineBehavior<PipelineRequest, string>, BehaviorA>();
        services.AddSingleton<ForgePipelineBehavior<PipelineRequest, string>, BehaviorB>();

        services.AddSingleton<IForgePreProcessor<PipelineRequest, string>, Pre1>();
        services.AddSingleton<IForgePreProcessor<PipelineRequest, string>, Pre2>();

        services.AddSingleton<IForgePostProcessor<PipelineRequest, string>, Post1>();
        services.AddSingleton<IForgePostProcessor<PipelineRequest, string>, Post2>();

        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IForgeMediator>();
        var collector = sp.GetRequiredService<Collector>();

        var response = await mediator.Send(new PipelineRequest());
        Assert.Equal("OK", response);

        var expected = new[]
        {
            "pre1", "pre2",
            "behaviorA-before", "behaviorB-before",
            "handler",
            "behaviorB-after", "behaviorA-after",
            "post1", "post2"
        };
        Assert.Equal(expected, collector.ToArray());
    }

    [Fact]
    public async Task Exception_Handler_Recovers_And_Skips_PostProcessors()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IForgeMediator, ForgeMediator>();
        services.AddSingleton<ForgeRequestHandler<FailingRequest, string>, FailingHandler>();
        services.AddSingleton<IForgeExceptionHandler<FailingRequest, string>, FailingRequestExceptionHandler>();
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IForgeMediator>();

        var response = await mediator.Send(new FailingRequest());
        Assert.Equal("RECOVERED", response);
    }

    [Fact]
    public async Task Unhandled_Exception_Bubbles()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IForgeMediator, ForgeMediator>();
        services.AddSingleton<ForgeRequestHandler<FailingRequest, string>, FailingHandler>();
        // No exception handler registered
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IForgeMediator>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.Send(new FailingRequest()));
    }
}
