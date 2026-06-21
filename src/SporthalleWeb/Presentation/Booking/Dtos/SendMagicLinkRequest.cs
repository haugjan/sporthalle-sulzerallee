namespace SporthalleWeb.Presentation.Booking.Dtos;

public sealed record SendMagicLinkRequest(string Email, string? CaptchaToken);
