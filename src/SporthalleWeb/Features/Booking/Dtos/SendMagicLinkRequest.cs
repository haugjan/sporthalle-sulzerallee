namespace SporthalleWeb.Features.Booking.Dtos;

public sealed record SendMagicLinkRequest(string Email, string? CaptchaToken);
