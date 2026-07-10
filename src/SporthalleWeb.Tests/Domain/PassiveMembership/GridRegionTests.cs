using SporthalleWeb.Domain.PassiveMembership.PassiveMemberAggregate;
using Xunit;

namespace SporthalleWeb.Tests.Domain.PassiveMembership;

public sealed class GridRegionTests
{
    [Fact]
    public void Create_ValidValues_KeepsThem()
    {
        var r = GridRegion.Create(0.1, 0.2, 0.8, 0.9);
        Assert.Equal(0.1, r.X0);
        Assert.Equal(0.2, r.Y0);
        Assert.Equal(0.8, r.X1);
        Assert.Equal(0.9, r.Y1);
    }

    [Fact]
    public void Create_OutOfRange_Clamps()
    {
        var r = GridRegion.Create(-0.5, -1, 2, 3);
        Assert.Equal(0.0, r.X0);
        Assert.Equal(0.0, r.Y0);
        Assert.Equal(1.0, r.X1);
        Assert.Equal(1.0, r.Y1);
    }

    [Theory]
    [InlineData(0.8, 0.2, 0.2, 0.9)] // x1 <= x0
    [InlineData(0.2, 0.9, 0.8, 0.2)] // y1 <= y0
    public void Create_InvertedOrDegenerate_FallsBackToDefault(double x0, double y0, double x1, double y1)
    {
        var r = GridRegion.Create(x0, y0, x1, y1);
        Assert.Equal(GridRegion.Default, r);
    }
}
