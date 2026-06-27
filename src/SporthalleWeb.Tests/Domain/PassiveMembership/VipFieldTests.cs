using SporthalleWeb.Features.PassiveMembership.Registration.PassiveMemberAggregate;
using Xunit;

namespace SporthalleWeb.Tests.Domain.PassiveMembership;

public sealed class VipFieldTests
{
    // fieldNumber = row * 20 + col + 1

    [Theory]
    [InlineData(5 * 20 + 1 + 1)]  // GoalCreaseLeft row=5, col=1
    [InlineData(9 * 20 + 3 + 1)]  // GoalCreaseLeft row=9, col=3
    [InlineData(5 * 20 + 16 + 1)] // GoalCreaseRight row=5, col=16
    [InlineData(9 * 20 + 18 + 1)] // GoalCreaseRight row=9, col=18
    [InlineData(5 * 20 + 8 + 1)]  // CenterCircle row=5, col=8
    [InlineData(9 * 20 + 11 + 1)] // CenterCircle row=9, col=11
    [InlineData(45)]               // FaceOffSpot left-top
    [InlineData(243)]              // FaceOffSpot left-bottom
    [InlineData(58)]               // FaceOffSpot right-top
    [InlineData(258)]              // FaceOffSpot right-bottom
    public void IsVip_VipField_ReturnsTrue(int fieldNumber)
    {
        Assert.True(VipField.IsVip(fieldNumber));
    }

    [Theory]
    [InlineData(1)]   // top-left corner, not VIP
    [InlineData(300)] // bottom-right corner, not VIP
    [InlineData(11)]  // random non-VIP
    public void IsVip_NonVipField_ReturnsFalse(int fieldNumber)
    {
        Assert.False(VipField.IsVip(fieldNumber));
    }

    [Fact]
    public void GetLabel_GoalCrease_ReturnsTorraum()
    {
        // col=2, row=7 → GoalCreaseLeft
        Assert.Equal("Torraum", VipField.GetLabel(7 * 20 + 2 + 1));
        // col=17, row=7 → GoalCreaseRight
        Assert.Equal("Torraum", VipField.GetLabel(7 * 20 + 17 + 1));
    }

    [Fact]
    public void GetLabel_CenterCircle_ReturnsAnspielkreis()
    {
        Assert.Equal("Anspielkreis", VipField.GetLabel(7 * 20 + 9 + 1));
    }

    [Fact]
    public void GetLabel_FaceOffSpot_ReturnsAnspielpunkt()
    {
        Assert.Equal("Anspielpunkt", VipField.GetLabel(45));
    }

    [Fact]
    public void GetLabel_NonVipField_ReturnsNull()
    {
        Assert.Null(VipField.GetLabel(1));
    }
}
