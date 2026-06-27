using SporthalleWeb.Features.Booking;

namespace SporthalleWeb.Features.Booking;

public sealed record BookingSlotDto(
    int Id,
    int? MemberId,
    string Type,
    DateTime StartUtc,
    DateTime EndUtc,
    string Title,
    string? Color,
    string? Notes)
{
    public static BookingSlotDto From(BookingSlot s) => new(
        s.Id, s.MemberId, s.Type.ToString(),
        s.Slot.StartUtc, s.Slot.EndUtc,
        s.Title, s.Color, s.Notes);
}
