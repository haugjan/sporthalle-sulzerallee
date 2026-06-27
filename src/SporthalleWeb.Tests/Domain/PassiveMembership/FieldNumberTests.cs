using SporthalleWeb.Features.PassiveMembership.Registration.PassiveMemberAggregate;
using Xunit;

namespace SporthalleWeb.Tests.Domain.PassiveMembership;

public sealed class FieldNumberTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(150)]
    [InlineData(300)]
    public void Constructor_ValidValue_SetsValue(int value)
    {
        var fn = new FieldNumber(value);
        Assert.Equal(value, fn.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(301)]
    [InlineData(1000)]
    public void Constructor_OutOfRange_ThrowsDomainException(int value)
    {
        Assert.Throws<DomainException>(() => new FieldNumber(value));
    }

    [Fact]
    public void Records_WithSameValue_AreEqual()
    {
        Assert.Equal(new FieldNumber(42), new FieldNumber(42));
    }

    [Fact]
    public void Records_WithDifferentValues_AreNotEqual()
    {
        Assert.NotEqual(new FieldNumber(1), new FieldNumber(2));
    }
}
