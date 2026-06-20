namespace SporthalleWeb.Presentation.Reservierung.Dtos;

public sealed record ResetPasswordRequest(int MemberId, string Token, string NewPassword);
