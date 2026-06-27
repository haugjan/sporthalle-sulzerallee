namespace SporthalleWeb.Features.Booking;

public sealed record ResetPasswordRequest(int MemberId, string Token, string NewPassword);
