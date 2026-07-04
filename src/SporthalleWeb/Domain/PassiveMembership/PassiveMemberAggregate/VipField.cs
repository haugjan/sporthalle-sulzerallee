namespace SporthalleWeb.Domain.PassiveMembership.PassiveMemberAggregate;

public static class VipField
{
    // VIP areas expressed as fractions [0,1] of the floor grid (x = column axis,
    // y = row axis). Defining them as fractions keeps them aligned with the goal
    // creases, centre circle and face-off spots painted on the floor image no
    // matter the grid resolution (see FloorGrid).
    private sealed record Region(string Label, double X0, double X1, double Y0, double Y1);

    private static readonly Region[] Regions =
    [
        new("Torraum",      0.05, 0.20, 1.0 / 3, 2.0 / 3), // left goal crease
        new("Torraum",      0.80, 0.95, 1.0 / 3, 2.0 / 3), // right goal crease
        new("Anspielkreis", 0.40, 0.60, 1.0 / 3, 2.0 / 3), // centre circle
        new("Anspielpunkt", 0.20, 0.25, 0.12, 0.21),       // face-off left top
        new("Anspielpunkt", 0.10, 0.15, 0.79, 0.88),       // face-off left bottom
        new("Anspielpunkt", 0.85, 0.90, 0.12, 0.21),       // face-off right top
        new("Anspielpunkt", 0.85, 0.90, 0.79, 0.88),       // face-off right bottom
    ];

    public static string? GetLabel(int fieldNumber)
    {
        if (fieldNumber < 1 || fieldNumber > FloorGrid.TotalFields) return null;

        var idx = fieldNumber - 1;
        var col = idx % FloorGrid.Columns;
        var row = idx / FloorGrid.Columns;
        var cx = (col + 0.5) / FloorGrid.Columns;
        var cy = (row + 0.5) / FloorGrid.Rows;

        foreach (var r in Regions)
            if (cx >= r.X0 && cx <= r.X1 && cy >= r.Y0 && cy <= r.Y1)
                return r.Label;

        return null;
    }

    public static bool IsVip(int fieldNumber) => GetLabel(fieldNumber) != null;
}
