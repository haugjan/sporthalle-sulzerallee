namespace SporthalleWeb.Features.PassiveMembership.Registration;

public record RegisterMemberRequest(
    int FieldNumber,
    string FirstName,
    string LastName,
    string Email,
    string LevelKey,
    bool ShowNameOnFloor,
    string? DisplayName,
    bool Consent,
    string CaptchaToken
);
