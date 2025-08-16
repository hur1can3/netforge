using NetForge.Core.Abstractions;
using NetForge.Core.Results;
using Microsoft.Extensions.DependencyInjection;

namespace NetForge.Tests.Core;

public class ForgeMediatorTests
{
    private sealed record Ping(string Message) : ForgeRequest<ForgeResult<string>>;

    private sealed class PingHandler : ForgeRequestHandler<Ping, ForgeResult<string>>
    {
        public override Task<ForgeResult<string>> Handle(Ping request, CancellationToken cancellationToken) =>
            Task.FromResult(ForgeResults.Success($"PONG:{request.Message}"));
    }

    [Fact]
    public async Task MediatorInvokesHandler()
    {
        var services = new ServiceCollection();
    services.AddSingleton<IForgeMediator, ForgeMediator>();
    services.AddSingleton<ForgeRequestHandler<Ping, ForgeResult<string>>, PingHandler>();
    var sp = services.BuildServiceProvider();
    var mediator = sp.GetRequiredService<IForgeMediator>();
    // Explicit resolve to ensure handler is instantiated (satisfies CA1812 intent)
    _ = sp.GetRequiredService<ForgeRequestHandler<Ping, ForgeResult<string>>>();
        var response = await mediator.Send(new Ping("HELLO"));
        Assert.True(response.IsSuccess);
        Assert.Equal("PONG:HELLO", response.Value);
    }
}
