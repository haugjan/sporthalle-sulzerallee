namespace SporthalleWeb.Presentation.Booking.Dtos;

public sealed record ResetPasswordRequest(int MemberId, string Token, string NewPassword);
