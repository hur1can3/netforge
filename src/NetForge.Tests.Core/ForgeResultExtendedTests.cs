using NetForge.Core.Results;
using Xunit;

namespace NetForge.Tests.Core;

public class ForgeResultExtendedTests
{
    [Fact]
    public void GenericSuccessStoresValue()
    {
        var r = ForgeResults.Success("val");
        Assert.True(r.IsSuccess);
        Assert.Equal("val", r.Value);
    }

    [Fact]
    public void GenericFailureStoresErrors()
    {
        var err = ForgeError.Unexpected("boom");
        var r = ForgeResults.Failure<string>(err);
        Assert.True(r.IsFailure);
        Assert.Single(r.Errors);
        Assert.Equal("boom", r.Errors[0].Message);
    }

    [Fact]
    public void Equality_Works_For_NonGeneric()
    {
        var a = ForgeResults.Success();
        var b = ForgeResults.Success();
        Assert.True(a==b);
    }

    [Fact]
    public void Equality_Works_For_Generic()
    {
        var a = ForgeResults.Success(5);
        var b = ForgeResults.Success(5);
        Assert.True(a==b);
    }
}
