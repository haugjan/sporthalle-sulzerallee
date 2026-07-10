using SporthalleWeb.Domain.PassiveMembership.PassiveMemberAggregate;

namespace SporthalleWeb.Features.PassiveMembership.Registration;

public sealed record FloorPlanSettings(
    string? BackgroundUrl,
    string? LineColor,
    GridRegion Region,
    SpecialFieldMap SpecialFields)
{
    public static FloorPlanSettings Default { get; } =
        new(null, null, GridRegion.Default, VipField.Default);
}

public interface IFloorPlanSettings
{
    Task<FloorPlanSettings> GetAsync();

    Task<string?> GetRawRasterAsync();
}
