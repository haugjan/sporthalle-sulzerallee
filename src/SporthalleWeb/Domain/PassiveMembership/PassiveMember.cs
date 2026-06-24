namespace SporthalleWeb.Domain.PassiveMembership;

public sealed class PassiveMember
{
    public int Id { get; private set; }
    public FieldNumber FieldNumber { get; private set; } = null!;
    public string FirstName { get; private set; } = "";
    public string LastName { get; private set; } = "";
    public string AddressLine { get; private set; } = "";
    public string? AddressLine2 { get; private set; }
    public string PostalCode { get; private set; } = "";
    public string City { get; private set; } = "";
    public string Country { get; private set; } = "Schweiz";
    public string? Phone { get; private set; }
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
    public DateTime? ExportedToAccountingAt { get; private set; }
    public string? ExportedToAccountingBy { get; private set; }
    public bool ExportedToAccounting => ExportedToAccountingAt.HasValue;
    public string? Notes { get; private set; }

    private PassiveMember() { }

    public static PassiveMember Register(
        FieldNumber fieldNumber,
        string firstName,
        string lastName,
        string addressLine,
        string? addressLine2,
        string postalCode,
        string city,
        string? phone,
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
            AddressLine2 = string.IsNullOrWhiteSpace(addressLine2) ? null : addressLine2.Trim(),
            PostalCode = postalCode.Trim(),
            City = city.Trim(),
            Country = "Schweiz",
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
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
        string addressLine, string? addressLine2, string postalCode, string city, string country,
        string? phone, string email, string levelKey,
        bool showNameOnFloor, string? displayName,
        DateTime createdAt, string status,
        DateTime? confirmedAt, string? confirmedBy,
        DateTime? paidAt, string? paidBy,
        DateTime? exportedToAccountingAt, string? exportedToAccountingBy,
        string? notes) => new PassiveMember
    {
        Id = id,
        FieldNumber = new FieldNumber(fieldNumber),
        FirstName = firstName,
        LastName = lastName,
        AddressLine = addressLine,
        AddressLine2 = addressLine2,
        PostalCode = postalCode,
        City = city,
        Country = country,
        Phone = phone,
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
        ExportedToAccountingAt = exportedToAccountingAt,
        ExportedToAccountingBy = exportedToAccountingBy,
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

    public void MarkAsExportedToAccounting(string by)
    {
        ExportedToAccountingAt = DateTime.UtcNow;
        ExportedToAccountingBy = by;
    }

    public void UnmarkAsExportedToAccounting()
    {
        ExportedToAccountingAt = null;
        ExportedToAccountingBy = null;
    }

    public void UpdateNotes(string? notes) => Notes = notes?.Trim();
}
