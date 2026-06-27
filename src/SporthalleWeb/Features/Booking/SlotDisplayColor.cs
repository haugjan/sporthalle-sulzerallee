using SporthalleWeb.Domain.Booking.SlotAggregate;

namespace SporthalleWeb.Features.Booking;

/// <summary>
/// Computes the colour a slot is displayed with in the calendar and lists.
/// The colour is no longer stored per slot: it comes from the renting hall member
/// (<c>HallMember.Color</c>). Blockers have no member and are always grey; any slot
/// without a member colour falls back to blue.
/// </summary>
public static class SlotDisplayColor
{
    /// <summary>Fallback for renter slots without a member colour (and new renters).</summary>
    public const string Blue = "#0078D4";

    /// <summary>Blockers are always shown grey.</summary>
    public const string BlockerGrey = "#78909C";

    public static string For(SlotType type, string? memberColor) =>
        type == SlotType.Blocker
            ? BlockerGrey
            : (string.IsNullOrWhiteSpace(memberColor) ? Blue : memberColor!.Trim());
}
