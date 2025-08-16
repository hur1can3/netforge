using NetForge.Core.Abstractions;
using NetForge.Core.Results;
using Microsoft.Extensions.DependencyInjection;

namespace NetForge.Tests.Core;

public class MediatorTests
{
    private sealed record Ping(string Message) : IRequest<Result<string>>;

    private sealed class PingHandler : IRequestHandler<Ping, Result<string>>
    {
        public Task<Result<string>> Handle(Ping request, CancellationToken cancellationToken) =>
            Task.FromResult(Result<string>.Success($"PONG:{request.Message}"));
    }

    [Fact]
    public async Task MediatorInvokesHandler()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMediator, Mediator>();
        services.AddSingleton<IRequestHandler<Ping, Result<string>>, PingHandler>();
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();
        var response = await mediator.Send(new Ping("HELLO"));
        Assert.True(response.IsSuccess);
        Assert.Equal("PONG:HELLO", response.Value);
    }
}
