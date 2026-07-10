namespace SporthalleWeb.Domain.PassiveMembership.PassiveMemberAggregate;

// A special ("VIP") area covers a rectangle of the floor grid, given by the field
// number of two opposite corners (From/To). A single field is a rectangle with
// From == To. Field numbers run 1..1000, 40 per row (see FloorGrid).
public sealed record SpecialArea(string Label, int From, int To);

public sealed class SpecialFieldMap
{
    private readonly IReadOnlyList<SpecialArea> _areas;

    public SpecialFieldMap(IReadOnlyList<SpecialArea> areas) => _areas = areas;

    public IReadOnlyList<SpecialArea> Areas => _areas;

    public string? GetLabel(int fieldNumber)
    {
        if (fieldNumber < 1 || fieldNumber > FloorGrid.TotalFields) return null;

        var col = (fieldNumber - 1) % FloorGrid.Columns;
        var row = (fieldNumber - 1) / FloorGrid.Columns;

        foreach (var area in _areas)
        {
            if (area.From < 1 || area.From > FloorGrid.TotalFields ||
                area.To < 1 || area.To > FloorGrid.TotalFields)
                continue;

            var c0 = (area.From - 1) % FloorGrid.Columns;
            var r0 = (area.From - 1) / FloorGrid.Columns;
            var c1 = (area.To - 1) % FloorGrid.Columns;
            var r1 = (area.To - 1) / FloorGrid.Columns;

            if (col >= Math.Min(c0, c1) && col <= Math.Max(c0, c1) &&
                row >= Math.Min(r0, r1) && row <= Math.Max(r0, r1))
                return area.Label;
        }

        return null;
    }

    public bool IsVip(int fieldNumber) => GetLabel(fieldNumber) != null;
}
