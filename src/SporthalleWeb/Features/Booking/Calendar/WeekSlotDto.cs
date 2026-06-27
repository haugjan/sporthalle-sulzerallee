namespace SporthalleWeb.Features.Booking.Calendar;

public sealed record WeekSlotDto(
    int Id,
    DateTime StartUtc,
    DateTime EndUtc,
    string Type,
    string? Color,
    string Title);
