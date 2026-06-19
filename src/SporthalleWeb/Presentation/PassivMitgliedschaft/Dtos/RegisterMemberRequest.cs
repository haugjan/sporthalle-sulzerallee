namespace SporthalleWeb.Presentation.PassivMitgliedschaft.Dtos;

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
    bool Consent
    // Phase 2: string CaptchaToken
);
