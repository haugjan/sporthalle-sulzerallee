namespace SporthalleWeb.Domain.PassiveMembership.PassiveMemberAggregate;

// Single source of truth for the floor plan grid resolution. Referenced by the
// domain (FieldNumber, VipField) and the presentation (FloorPlanComponent) so the
// field count stays consistent everywhere.
public static class FloorGrid
{
    public const int Columns = 40;
    public const int Rows = 25;
    public const int TotalFields = Columns * Rows; // 1000
}
