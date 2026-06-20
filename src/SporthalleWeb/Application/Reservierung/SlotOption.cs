namespace SporthalleWeb.Application.Reservierung;

public sealed record SlotOption(
    DateTime StartUtc,
    DateTime EndUtc,
    string StartLocal,
    string EndLocal,
    bool IsAvailable
);
