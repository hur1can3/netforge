using NetForge.Core.Results;

namespace NetForge.Tests.Core;

public class ForgeResultTests
{
    [Fact]
    public void Success_NoErrors()
    {
        var r = ForgeResult.Success();
        Assert.True(r.IsSuccess);
        Assert.Empty(r.Errors);
    }

    [Fact]
    public void Failure_WithErrors()
    {
        var r = ForgeResult.Failure(ForgeError.Unexpected("x"));
        Assert.True(r.IsFailure);
        Assert.Single(r.Errors);
    }
}
