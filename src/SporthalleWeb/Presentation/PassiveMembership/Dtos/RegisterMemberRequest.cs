namespace SporthalleWeb.Presentation.PassiveMembership.Dtos;

public record RegisterMemberRequest(
    int FieldNumber,
    string FirstName,
    string LastName,
    string AddressLine,
    string PostalCode,
    string City,
    string Email,
    string LevelKey,
    bool ShowNameOnFloor,
    string? DisplayName,
    bool Consent,
    string CaptchaToken
);
