namespace SporthalleWeb.Infrastructure.PassiveMembership;

/// <summary>
/// Single source of truth for all Umbraco member property aliases on the passivMember type.
/// Both <see cref="SporthalleWeb.MemberTypeSeeder"/> and <see cref="UmbracoPassiveMembers"/>
/// reference these constants, so a rename or typo is caught at compile time.
/// </summary>
internal static class PassivMemberAliases
{
    // Contact & membership
    internal const string Email             = "email";
    internal const string FirstName         = "firstName";
    internal const string LastName          = "lastName";
    internal const string FieldNumber       = "fieldNumber";
    internal const string MembershipLevel   = "membershipLevel";

    // Address
    internal const string BillingAddress    = "billingAddress";
    internal const string AddressLine2      = "addressLine2";
    internal const string BillingPostalCode = "billingPostalCode";
    internal const string BillingCity       = "billingCity";
    internal const string BillingCountry    = "billingCountry";
    internal const string Phone             = "phone";

    // Floor display
    internal const string ShowNameOnFloor   = "showNameOnFloor";
    internal const string FloorDisplayName  = "floorDisplayName";

    // Admin
    internal const string Status                   = "status";
    internal const string PaidAt                   = "paidAt";
    internal const string PaidBy                   = "paidBy";
    internal const string ConfirmedAt              = "confirmedAt";
    internal const string ConfirmedBy              = "confirmedBy";
    internal const string ExportedToAccountingAt   = "exportedToAccountingAt";
    internal const string ExportedToAccountingBy   = "exportedToAccountingBy";
    internal const string Notes                    = "notes";

    /// <summary>Complete set — mirrors <c>MemberTypeSeeder.EnsurePassivMemberType</c>.</summary>
    internal static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        Email, FirstName, LastName, FieldNumber, MembershipLevel,
        BillingAddress, AddressLine2, BillingPostalCode, BillingCity, BillingCountry, Phone,
        ShowNameOnFloor, FloorDisplayName,
        Status, PaidAt, PaidBy, ConfirmedAt, ConfirmedBy,
        ExportedToAccountingAt, ExportedToAccountingBy, Notes
    };
}
