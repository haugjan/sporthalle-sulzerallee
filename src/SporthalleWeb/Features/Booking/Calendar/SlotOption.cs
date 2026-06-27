namespace SporthalleWeb.Features.Booking.Calendar;

public sealed record SlotOption(
    DateTime StartUtc,
    DateTime EndUtc,
    string StartLocal,
    string EndLocal,
    bool IsAvailable
);
