using SporthalleWeb.Features.PassiveMembership.Registration.PassiveMemberAggregate;
using SporthalleWeb.Infrastructure.Booking;
using SporthalleWeb.Infrastructure.PassiveMembership;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

namespace SporthalleWeb;

public sealed class MemberTypeSeederComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, MemberTypeSeeder>();
}

public sealed class MemberTypeSeeder(
    IMemberTypeService memberTypeService,
    IDataTypeService dataTypeService,
    IShortStringHelper shortStringHelper,
    PropertyEditorCollection propertyEditors,
    IConfigurationEditorJsonSerializer serializer) : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    public Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        var all = dataTypeService.GetAll().ToList();

        var textBox = all.FirstOrDefault(d => d.EditorAlias == "Umbraco.TextBox")
            ?? throw new InvalidOperationException("Umbraco.TextBox data type not found.");

        var textArea = all.FirstOrDefault(d => d.EditorAlias == "Umbraco.TextArea")
            ?? all.FirstOrDefault(d => d.EditorAlias == "Umbraco.TextBox")
            ?? throw new InvalidOperationException("Text area data type not found.");

        var trueFalse = all.FirstOrDefault(d => d.EditorAlias == "Umbraco.TrueFalse")
            ?? throw new InvalidOperationException("Umbraco.TrueFalse data type not found.");

        var emailType = all.FirstOrDefault(d => d.EditorAlias == "Umbraco.EmailAddress")
            ?? throw new InvalidOperationException("Umbraco.EmailAddress data type not found.");

        var dateType = all.FirstOrDefault(d => d.EditorAlias == "Umbraco.DateTime")
            ?? throw new InvalidOperationException("Umbraco.DateTime data type not found.");

        var statusDropdown        = GetOrCreateDropdown(all, "Passive Member Status",
            new[] { MemberStatus.Pending, MemberStatus.Confirmed, MemberStatus.Deleted });

        var membershipLevelDropdown = GetOrCreateDropdown(all, "Passive Member Membership Level",
            new[] { "Bronze", "Silber", "Gold" });

        var renterTypeDropdown = GetOrCreateDropdown(all, "Hall Renter Type",
            new[] { "Privatperson", "Verein", "Firma", "Schule" });

        EnsureHallMemberType(textBox, textArea, trueFalse, renterTypeDropdown);
        EnsurePassivMemberType(textBox, textArea, trueFalse, emailType, dateType, statusDropdown, membershipLevelDropdown);

        return Task.CompletedTask;
    }

    private IDataType GetOrCreateDropdown(List<IDataType> all, string name, string[] values)
    {
        var existing = all.FirstOrDefault(d => d.Name == name);
        if (existing is not null) return existing;

        if (!propertyEditors.TryGet("Umbraco.DropDown.Flexible", out var editor))
            throw new InvalidOperationException("Umbraco.DropDown.Flexible property editor not found.");

        // Umbraco.DropDown.Flexible expects items as objects with 'id' and 'value'.
        var items = values.Select((v, i) => new Dictionary<string, object>
        {
            ["id"]    = i + 1,
            ["value"] = v
        }).ToList<object>();

        var dt = new DataType(editor, serializer)
        {
            Name         = name,
            DatabaseType = ValueStorageType.Nvarchar,
            ConfigurationData = new Dictionary<string, object>
            {
                ["multiple"] = false,
                ["items"]    = items
            }
        };
        dataTypeService.Save(dt);
        return dt;
    }

    // ── Hall Renter (hallMember) ──────────────────────────────────────────────

    private void EnsureHallMemberType(IDataType textBox, IDataType textArea, IDataType trueFalse, IDataType renterTypeDropdown)
    {
        const string alias = "hallMember";

        var memberType = memberTypeService.Get(alias);
        if (memberType is null)
        {
            memberType = new MemberType(shortStringHelper, -1)
            {
                Alias       = alias,
                Name        = "Hall Renter",
                Icon        = "icon-user",
                Description = "Mieter der Sporthalle Sulzerallee"
            };
        }

        const string g  = "renterInfo";
        const string gn = "Renter Info";

        EnsureProperty(memberType, renterTypeDropdown, HallMemberAliases.RenterType,        "Renter Type",         mandatory: true,  sort: 0,  g, gn);
        EnsureProperty(memberType, textBox,            HallMemberAliases.OrgName,           "Organisation / Name", mandatory: false, sort: 1,  g, gn);
        EnsureProperty(memberType, textBox,            HallMemberAliases.ContactFirstName,  "Contact First Name",  mandatory: true,  sort: 2,  g, gn);
        EnsureProperty(memberType, textBox,            HallMemberAliases.ContactLastName,   "Contact Last Name",   mandatory: true,  sort: 3,  g, gn);
        EnsureProperty(memberType, textBox,            HallMemberAliases.BillingAddress,    "Billing Address",     mandatory: true,  sort: 4,  g, gn);
        EnsureProperty(memberType, textBox,            HallMemberAliases.AddressLine2,      "Address Line 2",      mandatory: false, sort: 5,  g, gn);
        EnsureProperty(memberType, textBox,            HallMemberAliases.BillingPostalCode, "Billing Postal Code", mandatory: true,  sort: 6,  g, gn);
        EnsureProperty(memberType, textBox,            HallMemberAliases.BillingCity,       "Billing City",        mandatory: true,  sort: 7,  g, gn);
        EnsureProperty(memberType, textBox,            HallMemberAliases.BillingCountry,    "Billing Country",     mandatory: false, sort: 8,  g, gn);
        EnsureProperty(memberType, textBox,            HallMemberAliases.Phone,             "Phone",               mandatory: false, sort: 9,  g, gn);
        EnsureProperty(memberType, trueFalse,          HallMemberAliases.HasKey,            "Has Key",             mandatory: false, sort: 10, g, gn);
        EnsureProperty(memberType, textArea,           HallMemberAliases.Notes,             "Notes",               mandatory: false, sort: 11, g, gn);

        memberTypeService.Save(memberType);
    }

    // ── Passive Member (passivMember) ─────────────────────────────────────────

    private void EnsurePassivMemberType(
        IDataType textBox, IDataType textArea, IDataType trueFalse,
        IDataType emailType, IDataType dateType,
        IDataType statusDropdown, IDataType membershipLevelDropdown)
    {
        const string alias = "passivMember";

        var memberType = memberTypeService.Get(alias);
        if (memberType is null)
        {
            memberType = new MemberType(shortStringHelper, -1)
            {
                Alias       = alias,
                Name        = "Passive Member",
                Icon        = "icon-heart",
                Description = "Passivmitglied der Sporthalle Sulzerallee"
            };
        }

        const string infoG  = "passivInfo";
        const string infoGn = "Passive Member Info";
        const string adminG  = "passivAdmin";
        const string adminGn = "Admin";

        // Contact & membership
        EnsureProperty(memberType, emailType,               PassivMemberAliases.Email,           "E-Mail",             mandatory: true,  sort: 0,  infoG, infoGn);
        EnsureProperty(memberType, textBox,                 PassivMemberAliases.FirstName,       "First Name",         mandatory: true,  sort: 1,  infoG, infoGn);
        EnsureProperty(memberType, textBox,                 PassivMemberAliases.LastName,        "Last Name",          mandatory: true,  sort: 2,  infoG, infoGn);
        EnsureProperty(memberType, textBox,                 PassivMemberAliases.FieldNumber,     "Field Number",       mandatory: true,  sort: 3,  infoG, infoGn);
        EnsureProperty(memberType, membershipLevelDropdown, PassivMemberAliases.MembershipLevel, "Membership Level",   mandatory: true,  sort: 4,  infoG, infoGn);
        // Address — same aliases as hallMember
        EnsureProperty(memberType, textBox, PassivMemberAliases.BillingAddress,    "Billing Address",     mandatory: true,  sort: 5,  infoG, infoGn);
        EnsureProperty(memberType, textBox, PassivMemberAliases.AddressLine2,      "Address Line 2",      mandatory: false, sort: 6,  infoG, infoGn);
        EnsureProperty(memberType, textBox, PassivMemberAliases.BillingPostalCode, "Billing Postal Code", mandatory: true,  sort: 7,  infoG, infoGn);
        EnsureProperty(memberType, textBox, PassivMemberAliases.BillingCity,       "Billing City",        mandatory: true,  sort: 8,  infoG, infoGn);
        EnsureProperty(memberType, textBox, PassivMemberAliases.BillingCountry,    "Billing Country",     mandatory: false, sort: 9,  infoG, infoGn);
        EnsureProperty(memberType, textBox, PassivMemberAliases.Phone,             "Phone",               mandatory: false, sort: 10, infoG, infoGn);
        // Floor display
        EnsureProperty(memberType, trueFalse, PassivMemberAliases.ShowNameOnFloor,  "Show Name on Floor", mandatory: false, sort: 11, infoG, infoGn);
        EnsureProperty(memberType, textBox,   PassivMemberAliases.FloorDisplayName, "Floor Display Name", mandatory: false, sort: 12, infoG, infoGn);

        // Admin
        EnsureProperty(memberType, statusDropdown, PassivMemberAliases.Status,                 "Status",                    mandatory: false, sort: 0, adminG, adminGn);
        EnsureProperty(memberType, dateType,       PassivMemberAliases.PaidAt,                 "Paid At",                   mandatory: false, sort: 1, adminG, adminGn);
        EnsureProperty(memberType, textBox,        PassivMemberAliases.PaidBy,                 "Paid By",                   mandatory: false, sort: 2, adminG, adminGn);
        EnsureProperty(memberType, dateType,       PassivMemberAliases.ConfirmedAt,            "Confirmed At",              mandatory: false, sort: 3, adminG, adminGn);
        EnsureProperty(memberType, textBox,        PassivMemberAliases.ConfirmedBy,            "Confirmed By",              mandatory: false, sort: 4, adminG, adminGn);
        EnsureProperty(memberType, dateType,       PassivMemberAliases.ExportedToAccountingAt, "Exported to Accounting At", mandatory: false, sort: 5, adminG, adminGn);
        EnsureProperty(memberType, textBox,        PassivMemberAliases.ExportedToAccountingBy, "Exported to Accounting By", mandatory: false, sort: 6, adminG, adminGn);
        EnsureProperty(memberType, textArea,       PassivMemberAliases.Notes,                  "Notes",                     mandatory: false, sort: 7, adminG, adminGn);

        memberTypeService.Save(memberType);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    // Adds the property if missing; if it already exists with a different data type,
    // removes it and re-adds it (existing member values for that property are lost).
    // Always updates the display name.
    private void EnsureProperty(IMemberType memberType, IDataType dataType, string alias, string name,
        bool mandatory, int sort, string groupAlias, string groupName)
    {
        if (memberType.PropertyTypeExists(alias))
        {
            var existing = memberType.PropertyTypes.FirstOrDefault(p => p.Alias == alias);
            if (existing is null) return;

            existing.Name = name;

            if (existing.DataTypeKey == dataType.Key) return;

            // Data type changed — remove and re-add (member values for this property are lost).
            memberType.RemovePropertyType(alias);
        }

        memberType.AddPropertyType(Prop(dataType, alias, name, mandatory, sort), groupAlias, groupName);
    }

    private PropertyType Prop(IDataType dataType, string alias, string name, bool mandatory, int sortOrder)
        => new PropertyType(shortStringHelper, dataType, alias)
        {
            Name      = name,
            Mandatory = mandatory,
            SortOrder = sortOrder
        };
}
