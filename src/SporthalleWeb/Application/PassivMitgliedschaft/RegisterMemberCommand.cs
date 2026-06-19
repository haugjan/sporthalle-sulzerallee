namespace SporthalleWeb.Application.PassivMitgliedschaft;

public record RegisterMemberCommand(
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
    bool Consent);
