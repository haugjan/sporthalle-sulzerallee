namespace SporthalleWeb.Domain.PassiveMembership.PassiveMemberAggregate;

public static class VipField
{
    // The built-in default special-field layout, used when nothing is configured
    // on the floor plan element. Rectangles are given by their top-left/bottom-right
    // field number; a single field is a rectangle with From == To.
    public static SpecialFieldMap Default { get; } = new(
    [
        new SpecialArea("Torraum", 408, 611),
        new SpecialArea("Torraum", 434, 637),
        new SpecialArea("Mittelpunkt", 502, 502),
    ]);

    public static string? GetLabel(int fieldNumber) => Default.GetLabel(fieldNumber);

    public static bool IsVip(int fieldNumber) => Default.IsVip(fieldNumber);
}
