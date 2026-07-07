namespace SporthalleWeb.Domain.PassiveMembership.PassiveMemberAggregate;

public static class VipField
{
    // Special ("VIP") fields are derived geometrically from the real hall floor plan
    // markings. Geometry is expressed in normalised coordinates of the blue playing
    // surface: u runs along the hall length [0,1], v across the hall width [0,1].
    //
    // The surface is wider than it is tall, so a circle drawn in normalised (u,v)
    // space would come out as an ellipse. Aspect converts a v-distance into the u
    // scale (u = 1 spans the full width, v = 1 spans the full height) so that circle
    // tests stay round. It is the blue rectangle's height/width ratio in pixels.
    private const double Aspect = 2224.0 / 4628.0;

    // A circle contributes every cell inside it (a filled disc, RingInner == 0) or,
    // when RingInner == RingOuter, only the cells its outline crosses.
    private sealed record Circle(string Label, double U, double V, double RingInner, double RingOuter);
    private sealed record Rect(string Label, double U0, double U1, double V0, double V1);
    private sealed record Spot(string Label, double U, double V, double R);

    // ── Mittelkreise (centre circles of the two courts) ──────────────────────────
    // Filled discs: every field inside the centre circle is a special field.
    private const double CircleRadius = 0.170;
    private static readonly Circle[] Circles =
    [
        new("Mittelkreis", 0.298, 0.500, 0.0, CircleRadius),
        new("Mittelkreis", 0.758, 0.500, 0.0, CircleRadius),
    ];

    // ── Torfelder (goal creases at the two short ends) ───────────────────────────
    private static readonly Rect[] Goals =
    [
        new("Torraum", 0.020, 0.095, 0.360, 0.640),
        new("Torraum", 0.905, 0.980, 0.360, 0.640),
    ];

    // ── Eckanspielpunkte (four corner face-off spots) ────────────────────────────
    private const double SpotRadius = 0.022;
    private static readonly Spot[] Spots =
    [
        new("Anspielpunkt", 0.090, 0.110, SpotRadius),
        new("Anspielpunkt", 0.090, 0.890, SpotRadius),
        new("Anspielpunkt", 0.910, 0.110, SpotRadius),
        new("Anspielpunkt", 0.910, 0.890, SpotRadius),
    ];

    public static string? GetLabel(int fieldNumber)
    {
        if (fieldNumber < 1 || fieldNumber > FloorGrid.TotalFields) return null;

        var idx = fieldNumber - 1;
        var col = idx % FloorGrid.Columns;
        var row = idx / FloorGrid.Columns;

        var u0 = (double)col / FloorGrid.Columns;
        var u1 = (double)(col + 1) / FloorGrid.Columns;
        var v0 = (double)row / FloorGrid.Rows;
        var v1 = (double)(row + 1) / FloorGrid.Rows;

        foreach (var c in Circles)
            if (CellIntersectsAnnulus(u0, u1, v0, v1, c.U, c.V, c.RingInner, c.RingOuter))
                return c.Label;

        foreach (var g in Goals)
            if (u1 > g.U0 && u0 < g.U1 && v1 > g.V0 && v0 < g.V1)
                return g.Label;

        foreach (var s in Spots)
            if (CellIntersectsAnnulus(u0, u1, v0, v1, s.U, s.V, 0, s.R))
                return s.Label;

        return null;
    }

    public static bool IsVip(int fieldNumber) => GetLabel(fieldNumber) != null;

    private static bool CellIntersectsAnnulus(
        double u0, double u1, double v0, double v1,
        double cu, double cv, double rInner, double rOuter)
    {
        var nu = Math.Clamp(cu, u0, u1);
        var nv = Math.Clamp(cv, v0, v1);
        var dMin = Distance(cu - nu, cv - nv);

        var dMax = Math.Max(
            Math.Max(Distance(cu - u0, cv - v0), Distance(cu - u1, cv - v0)),
            Math.Max(Distance(cu - u0, cv - v1), Distance(cu - u1, cv - v1)));

        return dMin <= rOuter && dMax >= rInner;
    }

    private static double Distance(double du, double dv)
    {
        var dvy = dv * Aspect;
        return Math.Sqrt(du * du + dvy * dvy);
    }
}
