using SporthalleWeb.Domain.Booking.SlotAggregate;

namespace SporthalleWeb.Features.Booking;

public static class SlotDisplayColor
{
    public const string Blue = "#0078D4";

    public const string BlockerGrey = "#78909C";

    public static string For(SlotType type, string? memberColor) =>
        type == SlotType.Blocker
            ? BlockerGrey
            : (string.IsNullOrWhiteSpace(memberColor) ? Blue : memberColor!.Trim());
}
