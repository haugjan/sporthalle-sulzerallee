using SporthalleWeb.Domain.Booking;

namespace SporthalleWeb.Features.Booking;

public sealed record SendMagicLinkRequest(string Email, string? CaptchaToken);
