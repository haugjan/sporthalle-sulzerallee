namespace SporthalleWeb.Application.Booking;

public sealed record WeekSlotDto(
    int Id,
    DateTime StartUtc,
    DateTime EndUtc,
    string Type,
    string? Color,
    string Title);
