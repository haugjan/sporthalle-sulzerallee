namespace SporthalleWeb.Application.Reservierung;

public sealed record WeekSlotDto(
    int Id,
    DateTime StartUtc,
    DateTime EndUtc,
    string Status,
    string? Color,
    bool IsRecurringSlot,
    string? EventType);
