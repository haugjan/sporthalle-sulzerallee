namespace SporthalleWeb.Features.Booking.Dtos;

public sealed record CreateBookingRequest(
    DateTime StartUtc,
    DateTime EndUtc,
    string Title,
    string? Notes);
