namespace SporthalleWeb.Presentation.PassiveMembership.Dtos;

public record RegisterMemberRequest(
    int FieldNumber,
    string FirstName,
    string LastName,
    string AddressLine,
    string? AddressLine2,
    string PostalCode,
    string City,
    string? Phone,
    string Email,
    string LevelKey,
    bool ShowNameOnFloor,
    string? DisplayName,
    bool Consent,
    string CaptchaToken
);
