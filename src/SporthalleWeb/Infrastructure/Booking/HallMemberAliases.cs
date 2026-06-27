
using SporthalleWeb.Domain.Booking;

namespace SporthalleWeb.Infrastructure.Booking;

/// <summary>
/// Single source of truth for all Umbraco member property aliases on the hallMember type.
/// Both <see cref="SporthalleWeb.MemberTypeSeeder"/> and <see cref="UmbracoHallMembers"/>
/// reference these constants, so a rename or typo is caught at compile time.
/// </summary>
internal static class HallMemberAliases
{
    internal const string RenterType        = "renterType";
    internal const string OrgName           = "orgName";
    internal const string ContactFirstName  = "contactFirstName";
    internal const string ContactLastName   = "contactLastName";
    internal const string BillingAddress    = "billingAddress";
    internal const string AddressLine2      = "addressLine2";
    internal const string BillingPostalCode = "billingPostalCode";
    internal const string BillingCity       = "billingCity";
    internal const string BillingCountry    = "billingCountry";
    internal const string Phone             = "phone";
    internal const string HasKey            = "hasKey";
    internal const string Notes             = "notes";

    /// <summary>Complete set — mirrors <c>MemberTypeSeeder.EnsureHallMemberType</c>.</summary>
    internal static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        RenterType, OrgName, ContactFirstName, ContactLastName,
        BillingAddress, AddressLine2, BillingPostalCode, BillingCity, BillingCountry, Phone,
        HasKey, Notes
    };

    /// <summary>
    /// Aliases written during registration/profile update (not <c>Notes</c>,
    /// which is set by admins in the backoffice).
    /// </summary>
    internal static readonly IReadOnlySet<string> WrittenByAdapter = new HashSet<string>
    {
        RenterType, OrgName, ContactFirstName, ContactLastName,
        BillingAddress, AddressLine2, BillingPostalCode, BillingCity, BillingCountry, Phone,
        HasKey
    };
}
