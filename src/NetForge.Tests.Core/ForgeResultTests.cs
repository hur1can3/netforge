using NetForge.Core.Results;

namespace NetForge.Tests.Core;

public class ForgeResultTests
{
    [Fact]
    public void SuccessNoErrors()
    {
    var r = ForgeResults.Success();
        Assert.True(r.IsSuccess);
        Assert.Empty(r.Errors);
    }

    [Fact]
    public void FailureWithErrors()
    {
    var r = ForgeResults.Failure(ForgeError.Unexpected("x"));
        Assert.True(r.IsFailure);
        Assert.Single(r.Errors);
    }
}
