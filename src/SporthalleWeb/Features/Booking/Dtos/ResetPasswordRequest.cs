namespace SporthalleWeb.Features.Booking.Dtos;

public sealed record ResetPasswordRequest(int MemberId, string Token, string NewPassword);
