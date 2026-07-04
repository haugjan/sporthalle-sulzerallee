using SporthalleWeb.Domain.PassiveMembership.PassiveMemberAggregate;
using Xunit;

namespace SporthalleWeb.Tests.Domain.PassiveMembership;

public sealed class VipFieldTests
{
    // 40 columns × 25 rows grid → fieldNumber = row * 40 + col + 1

    [Theory]
    [InlineData(8 * 40 + 2 + 1)]   // Torraum left (col 2, row 8)
    [InlineData(16 * 40 + 7 + 1)]  // Torraum left (col 7, row 16)
    [InlineData(8 * 40 + 37 + 1)]  // Torraum right (col 37, row 8)
    [InlineData(8 * 40 + 16 + 1)]  // Anspielkreis (col 16, row 8)
    [InlineData(16 * 40 + 23 + 1)] // Anspielkreis (col 23, row 16)
    [InlineData(3 * 40 + 8 + 1)]   // Anspielpunkt left-top (col 8, row 3)
    public void IsVip_VipField_ReturnsTrue(int fieldNumber)
    {
        Assert.True(VipField.IsVip(fieldNumber));
    }

    [Theory]
    [InlineData(1)]    // top-left corner, not VIP
    [InlineData(1000)] // bottom-right corner, not VIP
    [InlineData(21)]   // top edge, not VIP
    public void IsVip_NonVipField_ReturnsFalse(int fieldNumber)
    {
        Assert.False(VipField.IsVip(fieldNumber));
    }

    [Fact]
    public void GetLabel_GoalCrease_ReturnsTorraum()
    {
        Assert.Equal("Torraum", VipField.GetLabel(8 * 40 + 2 + 1));   // left
        Assert.Equal("Torraum", VipField.GetLabel(8 * 40 + 37 + 1));  // right
    }

    [Fact]
    public void GetLabel_CenterCircle_ReturnsAnspielkreis()
    {
        Assert.Equal("Anspielkreis", VipField.GetLabel(8 * 40 + 16 + 1));
    }

    [Fact]
    public void GetLabel_FaceOffSpot_ReturnsAnspielpunkt()
    {
        Assert.Equal("Anspielpunkt", VipField.GetLabel(3 * 40 + 8 + 1));
    }

    [Fact]
    public void GetLabel_NonVipField_ReturnsNull()
    {
        Assert.Null(VipField.GetLabel(1));
    }
}
