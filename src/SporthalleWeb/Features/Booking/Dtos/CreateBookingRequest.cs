namespace SporthalleWeb.Features.Booking;

public sealed record CreateBookingRequest(
    DateTime StartUtc,
    DateTime EndUtc,
    string Title,
    string? Notes);
