using SporthalleWeb.Domain.PassiveMembership.PassiveMemberAggregate;
using Xunit;

namespace SporthalleWeb.Tests.Domain.PassiveMembership;

public sealed class VipFieldTests
{
    // Special fields are defined explicitly in VipField.cs:
    //   Rectangle 408..611, Rectangle 434..637, single field 502.

    [Theory]
    [InlineData(408)] // top-left corner of the left rectangle
    [InlineData(611)] // bottom-right corner of the left rectangle
    [InlineData(490)] // inside the left rectangle
    [InlineData(434)] // top-left corner of the right rectangle
    [InlineData(637)] // bottom-right corner of the right rectangle
    [InlineData(515)] // inside the right rectangle
    public void GetLabel_RectangleField_ReturnsTorraum(int fieldNumber)
    {
        Assert.Equal("Torraum", VipField.GetLabel(fieldNumber));
        Assert.True(VipField.IsVip(fieldNumber));
    }

    [Fact]
    public void GetLabel_CentreField_ReturnsMittelpunkt()
    {
        Assert.Equal("Mittelpunkt", VipField.GetLabel(502));
        Assert.True(VipField.IsVip(502));
    }

    [Theory]
    [InlineData(1)]    // corner, outside every special area
    [InlineData(407)]  // one column left of the left rectangle
    [InlineData(501)]  // next to the centre field but not special
    [InlineData(1000)] // opposite corner
    public void IsVip_PlainField_ReturnsFalse(int fieldNumber)
    {
        Assert.False(VipField.IsVip(fieldNumber));
        Assert.Null(VipField.GetLabel(fieldNumber));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1001)]
    public void GetLabel_OutOfRange_ReturnsNull(int fieldNumber)
    {
        Assert.Null(VipField.GetLabel(fieldNumber));
        Assert.False(VipField.IsVip(fieldNumber));
    }
}
