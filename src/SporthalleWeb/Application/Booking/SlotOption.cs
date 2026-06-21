namespace SporthalleWeb.Application.Booking;

public sealed record SlotOption(
    DateTime StartUtc,
    DateTime EndUtc,
    string StartLocal,
    string EndLocal,
    bool IsAvailable
);
