using SporthalleWeb.Domain.PassiveMembership.PassiveMemberAggregate;
using Xunit;

namespace SporthalleWeb.Tests.Domain.PassiveMembership;

public sealed class VipFieldTests
{
    // Sample fields derived from the geometric VIP definition in VipField.cs.
    // If the marking geometry (circle radius, goal/spot positions) is retuned,
    // these samples must be recomputed.

    [Theory]
    [InlineData(131)] // on the left Mittelkreis ring
    [InlineData(149)] // on the left Mittelkreis ring
    public void IsVip_CentreCircleRing_ReturnsTrue(int fieldNumber)
    {
        Assert.True(VipField.IsVip(fieldNumber));
        Assert.Equal("Mittelkreis", VipField.GetLabel(fieldNumber));
    }

    [Theory]
    [InlineData(361)] // inside the left Torraum
    [InlineData(398)] // inside the left Torraum
    public void GetLabel_GoalCrease_ReturnsTorraum(int fieldNumber)
    {
        Assert.Equal("Torraum", VipField.GetLabel(fieldNumber));
    }

    [Theory]
    [InlineData(43)] // top-left corner face-off spot
    [InlineData(76)]
    public void GetLabel_FaceOffSpot_ReturnsAnspielpunkt(int fieldNumber)
    {
        Assert.Equal("Anspielpunkt", VipField.GetLabel(fieldNumber));
    }

    [Theory]
    [InlineData(1)]     // extreme corner, outside every special area
    [InlineData(500)]   // open floor between the two centre circles
    [InlineData(1000)]  // opposite extreme corner
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
