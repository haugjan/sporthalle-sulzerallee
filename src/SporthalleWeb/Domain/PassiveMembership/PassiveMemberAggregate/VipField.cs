namespace SporthalleWeb.Domain.PassiveMembership.PassiveMemberAggregate;

public static class VipField
{
    // Special ("VIP") fields are chosen field-by-field on the numbered floor plan.
    // A rectangle is defined by its top-left and bottom-right field number; single
    // fields stand alone. Field numbers run 1..1000, 40 per row (see FloorGrid).

    private sealed record FieldRect(string Label, int TopLeft, int BottomRight);

    private static readonly FieldRect[] Rectangles =
    [
        new("Torraum", 408, 611),
        new("Torraum", 434, 637),
    ];

    private static readonly Dictionary<int, string> SingleFields = new()
    {
        [502] = "Mittelpunkt",
    };

    public static string? GetLabel(int fieldNumber)
    {
        if (fieldNumber < 1 || fieldNumber > FloorGrid.TotalFields) return null;

        var col = (fieldNumber - 1) % FloorGrid.Columns;
        var row = (fieldNumber - 1) / FloorGrid.Columns;

        foreach (var rect in Rectangles)
        {
            var c0 = (rect.TopLeft - 1) % FloorGrid.Columns;
            var r0 = (rect.TopLeft - 1) / FloorGrid.Columns;
            var c1 = (rect.BottomRight - 1) % FloorGrid.Columns;
            var r1 = (rect.BottomRight - 1) / FloorGrid.Columns;
            if (col >= c0 && col <= c1 && row >= r0 && row <= r1)
                return rect.Label;
        }

        return SingleFields.TryGetValue(fieldNumber, out var label) ? label : null;
    }

    public static bool IsVip(int fieldNumber) => GetLabel(fieldNumber) != null;
}
