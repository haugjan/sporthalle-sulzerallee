namespace SporthalleWeb.Domain.PassivMitgliedschaft;

public sealed class PassivMitglied
{
    public int Id { get; private set; }
    public FieldNumber FieldNumber { get; private set; } = null!;
    public string FirstName { get; private set; } = "";
    public string LastName { get; private set; } = "";
    public string AddressLine { get; private set; } = "";
    public string PostalCode { get; private set; } = "";
    public string City { get; private set; } = "";
    public string Country { get; private set; } = "Schweiz";
    public MemberEmail Email { get; private set; } = null!;
    public MembershipLevel Level { get; private set; } = null!;
    public bool ShowNameOnFloor { get; private set; }
    public string? DisplayName { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public string? Notes { get; private set; }

    private PassivMitglied() { }

    public static PassivMitglied Register(
        FieldNumber fieldNumber,
        string firstName,
        string lastName,
        string addressLine,
        string postalCode,
        string city,
        MemberEmail email,
        MembershipLevel level,
        bool showNameOnFloor,
        string? displayName)
    {
        if (showNameOnFloor && string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("Anzeigename erforderlich, wenn Name sichtbar sein soll.");

        return new PassivMitglied
        {
            FieldNumber = fieldNumber,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            AddressLine = addressLine.Trim(),
            PostalCode = postalCode.Trim(),
            City = city.Trim(),
            Country = "Schweiz",
            Email = email,
            Level = level,
            ShowNameOnFloor = showNameOnFloor,
            DisplayName = showNameOnFloor ? displayName!.Trim() : null,
            CreatedAt = DateTime.UtcNow
        };
    }

    // Wird vom Repository genutzt, um Domain-Objekte aus DB-Records zu rekonstruieren.
    public static PassivMitglied Reconstitute(
        int id, int fieldNumber, string firstName, string lastName,
        string addressLine, string postalCode, string city, string country,
        string email, string levelKey,
        bool showNameOnFloor, string? displayName,
        DateTime createdAt, DateTime? paidAt, string? notes) => new()
    {
        Id = id,
        FieldNumber = new FieldNumber(fieldNumber),
        FirstName = firstName,
        LastName = lastName,
        AddressLine = addressLine,
        PostalCode = postalCode,
        City = city,
        Country = country,
        Email = new MemberEmail(email),
        Level = MembershipLevel.FromKey(levelKey),
        ShowNameOnFloor = showNameOnFloor,
        DisplayName = displayName,
        CreatedAt = createdAt,
        PaidAt = paidAt,
        Notes = notes
    };

    public void MarkAsPaid() => PaidAt = DateTime.UtcNow;
    public void UpdateNotes(string? notes) => Notes = notes?.Trim();
}
