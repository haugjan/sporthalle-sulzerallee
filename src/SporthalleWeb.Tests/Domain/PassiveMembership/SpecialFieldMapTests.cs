using SporthalleWeb.Domain.PassiveMembership.PassiveMemberAggregate;
using Xunit;

namespace SporthalleWeb.Tests.Domain.PassiveMembership;

public sealed class SpecialFieldMapTests
{
    [Theory]
    [InlineData(408)] // top-left corner
    [InlineData(611)] // bottom-right corner
    [InlineData(490)] // inside
    public void RectangleArea_MembersReturnLabel(int fieldNumber)
    {
        var map = new SpecialFieldMap([new SpecialArea("Torraum", 408, 611)]);
        Assert.Equal("Torraum", map.GetLabel(fieldNumber));
        Assert.True(map.IsVip(fieldNumber));
    }

    [Fact]
    public void SingleField_ReturnsLabel()
    {
        var map = new SpecialFieldMap([new SpecialArea("Mittelpunkt", 502, 502)]);
        Assert.Equal("Mittelpunkt", map.GetLabel(502));
        Assert.False(map.IsVip(501));
    }

    [Fact]
    public void Corners_MayBeGivenInAnyOrder()
    {
        var map = new SpecialFieldMap([new SpecialArea("X", 611, 408)]);
        Assert.True(map.IsVip(490));
    }

    [Fact]
    public void EmptyMap_NothingIsSpecial()
    {
        var map = new SpecialFieldMap([]);
        Assert.False(map.IsVip(1));
        Assert.False(map.IsVip(502));
        Assert.Null(map.GetLabel(408));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1001)]
    public void OutOfRange_ReturnsNull(int fieldNumber)
    {
        Assert.Null(VipField.Default.GetLabel(fieldNumber));
        Assert.False(VipField.Default.IsVip(fieldNumber));
    }

    [Fact]
    public void Default_MatchesTheBuiltInLayout()
    {
        Assert.Equal("Torraum", VipField.Default.GetLabel(408));
        Assert.Equal("Torraum", VipField.Default.GetLabel(434));
        Assert.Equal("Mittelpunkt", VipField.Default.GetLabel(502));
        Assert.False(VipField.Default.IsVip(1));
    }
}
