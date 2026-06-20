namespace SporthalleWeb.Presentation.Reservierung.Dtos;

public sealed record CreateBookingRequest(
    DateTime StartUtc,
    DateTime EndUtc,
    string Title,
    string? Notes);
