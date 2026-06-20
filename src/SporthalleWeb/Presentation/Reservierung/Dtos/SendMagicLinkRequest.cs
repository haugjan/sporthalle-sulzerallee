namespace SporthalleWeb.Presentation.Reservierung.Dtos;

public sealed record SendMagicLinkRequest(string Email, string? CaptchaToken);
