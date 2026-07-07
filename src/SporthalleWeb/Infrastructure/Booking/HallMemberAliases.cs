
using SporthalleWeb.Domain.Booking;

namespace SporthalleWeb.Infrastructure.Booking;

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

    internal const string Color             = "color";

    internal static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        RenterType, OrgName, ContactFirstName, ContactLastName,
        BillingAddress, AddressLine2, BillingPostalCode, BillingCity, BillingCountry, Phone,
        HasKey, Notes, Color
    };

    internal static readonly IReadOnlySet<string> WrittenByAdapter = new HashSet<string>
    {
        RenterType, OrgName, ContactFirstName, ContactLastName,
        BillingAddress, AddressLine2, BillingPostalCode, BillingCity, BillingCountry, Phone,
        HasKey
    };
}
