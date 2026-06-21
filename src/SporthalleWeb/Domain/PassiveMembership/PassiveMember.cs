namespace SporthalleWeb.Domain.PassiveMembership;

public sealed class PassiveMember
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
    public string Status { get; private set; } = MemberStatus.Pending;
    public DateTime? ConfirmedAt { get; private set; }
    public string? ConfirmedBy { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public string? PaidBy { get; private set; }
    public bool ExportedToAccounting { get; private set; }
    public string? Notes { get; private set; }

    private PassiveMember() { }

    public static PassiveMember Register(
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

        return new PassiveMember
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
            CreatedAt = DateTime.UtcNow,
            Status = MemberStatus.Pending,
        };
    }

    public static PassiveMember Reconstitute(
        int id, int fieldNumber, string firstName, string lastName,
        string addressLine, string postalCode, string city, string country,
        string email, string levelKey,
        bool showNameOnFloor, string? displayName,
        DateTime createdAt, string status,
        DateTime? confirmedAt, string? confirmedBy,
        DateTime? paidAt, string? paidBy,
        bool exportedToAccounting, string? notes) => new PassiveMember
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
        Status = status,
        ConfirmedAt = confirmedAt,
        ConfirmedBy = confirmedBy,
        PaidAt = paidAt,
        PaidBy = paidBy,
        ExportedToAccounting = exportedToAccounting,
        Notes = notes,
    };

    public void Confirm(string confirmedBy, bool isPaid, string? paidBy)
    {
        Status = MemberStatus.Confirmed;
        ConfirmedAt = DateTime.UtcNow;
        ConfirmedBy = confirmedBy;
        if (isPaid)
        {
            PaidAt = DateTime.UtcNow;
            PaidBy = paidBy;
        }
    }

    public void SoftDelete() => Status = MemberStatus.Deleted;

    public void MarkAsPaid(string paidBy)
    {
        PaidAt = DateTime.UtcNow;
        PaidBy = paidBy;
    }

    public void MarkAsUnpaid()
    {
        PaidAt = null;
        PaidBy = null;
    }

    public void SetExportedToAccounting(bool value) => ExportedToAccounting = value;

    public void UpdateNotes(string? notes) => Notes = notes?.Trim();
}
