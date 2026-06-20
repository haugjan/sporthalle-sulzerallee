namespace SporthalleWeb.Application.Reservierung;

public sealed record CreateBookingCommand(
    int MemberId,
    DateTime StartUtc,
    DateTime EndUtc,
    string Title,
    string? Notes
);
