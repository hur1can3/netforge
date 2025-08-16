using NetForge.Core.Abstractions;
using NetForge.Core.Results;
using Microsoft.Extensions.DependencyInjection;

namespace NetForge.Tests.Core;

public class MediatorTests
{
    private sealed record Ping(string Message) : IForgeRequest<ForgeResult<string>>;

    private sealed class PingHandler : IForgeRequestHandler<Ping, ForgeResult<string>>
    {
        public Task<ForgeResult<string>> Handle(Ping request, CancellationToken cancellationToken) =>
            Task.FromResult(ForgeResult<string>.Success($"PONG:{request.Message}"));
    }

    [Fact]
    public async Task MediatorInvokesHandler()
    {
        var services = new ServiceCollection();
    services.AddSingleton<IForgeMediator, ForgeMediator>();
    services.AddSingleton<IForgeRequestHandler<Ping, ForgeResult<string>>, PingHandler>();
    var sp = services.BuildServiceProvider();
    var mediator = sp.GetRequiredService<IForgeMediator>();
        var response = await mediator.Send(new Ping("HELLO"));
        Assert.True(response.IsSuccess);
        Assert.Equal("PONG:HELLO", response.Value);
    }
}
