using SporthalleWeb.Domain.Booking;

namespace SporthalleWeb.Features.Booking;

public sealed record WeekSlotDto(
    int Id,
    DateTime StartUtc,
    DateTime EndUtc,
    string Type,
    string? Color,
    string Title);
