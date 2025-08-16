using NetForge.Core.Utilities;
using Xunit;

namespace NetForge.Tests.Core;

public class ForgeGuardTests
{
    [Fact]
    public void AgainstNull_ReturnsValue()
    {
        var obj = new object();
        var result = ForgeGuard.AgainstNull(obj, nameof(obj));
        Assert.Same(obj, result);
    }

    [Fact]
    public void AgainstNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ForgeGuard.AgainstNull<object?>(null, "x"));
    }

    [Fact]
    public void AgainstNullOrEmpty_Returns()
    {
        var s = "abc";
        var result = ForgeGuard.AgainstNullOrEmpty(s, nameof(s));
        Assert.Equal(s, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void AgainstNullOrEmpty_Throws(string? input)
    {
        Assert.ThrowsAny<ArgumentException>(() => ForgeGuard.AgainstNullOrEmpty(input, "p"));
    }

    [Fact]
    public void AgainstNonPositive_Returns()
    {
        var v = ForgeGuard.AgainstNonPositive(1, "n");
        Assert.Equal(1, v);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void AgainstNonPositive_Throws(int input)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ForgeGuard.AgainstNonPositive(input, "n"));
    }
}
