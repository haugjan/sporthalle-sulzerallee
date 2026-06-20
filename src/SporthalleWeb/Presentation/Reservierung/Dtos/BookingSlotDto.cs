using SporthalleWeb.Domain.Reservierung;

namespace SporthalleWeb.Presentation.Reservierung.Dtos;

public sealed record BookingSlotDto(
    int Id,
    int? MemberId,
    string Status,
    DateTime StartUtc,
    DateTime EndUtc,
    string? EventType,
    string? Notes,
    decimal? TotalPrice,
    string? Color,
    bool IsRecurringSlot)
{
    public static BookingSlotDto From(BookingSlot s) => new(
        s.Id, s.MemberId, s.Status.ToString(),
        s.Slot.StartUtc, s.Slot.EndUtc,
        s.EventType, s.Notes, s.TotalPrice, s.Color, s.IsRecurringSlot);
}
