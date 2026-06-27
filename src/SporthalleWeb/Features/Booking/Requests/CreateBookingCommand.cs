namespace SporthalleWeb.Features.Booking.Requests;

public sealed record CreateBookingCommand(
    int MemberId,
    DateTime StartUtc,
    DateTime EndUtc,
    string Title,
    string? Notes,
    string? Color = null
);
