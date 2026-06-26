using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
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
    IShortStringHelper shortStringHelper) : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    public Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        var textBox = dataTypeService.GetAll()
            .FirstOrDefault(d => d.EditorAlias == "Umbraco.TextBox")
            ?? throw new InvalidOperationException("Umbraco.TextBox data type not found.");

        var textArea = dataTypeService.GetAll()
            .FirstOrDefault(d => d.EditorAlias == "Umbraco.TextArea")
            ?? dataTypeService.GetAll().FirstOrDefault(d => d.EditorAlias == "Umbraco.TextBox")
            ?? throw new InvalidOperationException("Text area data type not found.");

        var trueFalse = dataTypeService.GetAll()
            .FirstOrDefault(d => d.EditorAlias == "Umbraco.TrueFalse")
            ?? throw new InvalidOperationException("Umbraco.TrueFalse data type not found.");

        EnsureHallMemberType(textBox, textArea, trueFalse);
        EnsurePassivMemberType(textBox, textArea, trueFalse);

        return Task.CompletedTask;
    }

    // ── Hall Renter (hallMember) ──────────────────────────────────────────────

    private void EnsureHallMemberType(IDataType textBox, IDataType textArea, IDataType trueFalse)
    {
        const string alias = "hallMember";

        var memberType = memberTypeService.Get(alias);
        if (memberType is null)
        {
            memberType = new MemberType(shortStringHelper, -1)
            {
                Alias = alias,
                Name = "Hall Renter",
                Icon = "icon-user",
                Description = "Mieter der Sporthalle Sulzerallee"
            };
        }

        const string g = "renterInfo";
        const string gn = "Renter Info";

        AddIfMissing(memberType, textBox,   "renterType",        "Renter Type",         mandatory: true,  sort: 0,  g, gn);
        AddIfMissing(memberType, textBox,   "orgName",           "Organisation / Name", mandatory: false, sort: 1,  g, gn);
        AddIfMissing(memberType, textBox,   "contactFirstName",  "Contact First Name",  mandatory: true,  sort: 2,  g, gn);
        AddIfMissing(memberType, textBox,   "contactLastName",   "Contact Last Name",   mandatory: true,  sort: 3,  g, gn);
        AddIfMissing(memberType, textBox,   "billingAddress",    "Billing Address",     mandatory: true,  sort: 4,  g, gn);
        AddIfMissing(memberType, textBox,   "addressLine2",      "Address Line 2",      mandatory: false, sort: 5,  g, gn);
        AddIfMissing(memberType, textBox,   "billingPostalCode", "Billing Postal Code", mandatory: true,  sort: 6,  g, gn);
        AddIfMissing(memberType, textBox,   "billingCity",       "Billing City",        mandatory: true,  sort: 7,  g, gn);
        AddIfMissing(memberType, textBox,   "billingCountry",    "Billing Country",     mandatory: true,  sort: 8,  g, gn);
        AddIfMissing(memberType, textBox,   "phone",             "Phone",               mandatory: false, sort: 9,  g, gn);
        AddIfMissing(memberType, trueFalse, "hasKey",            "Has Key",             mandatory: false, sort: 10, g, gn);
        AddIfMissing(memberType, textArea,  "notes",             "Notes",               mandatory: false, sort: 11, g, gn);

        memberTypeService.Save(memberType);
    }

    // ── Passive Member (passivMember) ─────────────────────────────────────────

    private void EnsurePassivMemberType(IDataType textBox, IDataType textArea, IDataType trueFalse)
    {
        const string alias = "passivMember";

        var memberType = memberTypeService.Get(alias);
        if (memberType is null)
        {
            memberType = new MemberType(shortStringHelper, -1)
            {
                Alias = alias,
                Name = "Passive Member",
                Icon = "icon-heart",
                Description = "Passivmitglied der Sporthalle Sulzerallee"
            };
        }

        const string infoG  = "passivInfo";
        const string infoGn = "Passive Member Info";
        const string adminG  = "passivAdmin";
        const string adminGn = "Admin";

        // Contact & membership
        AddIfMissing(memberType, textBox,   "email",           "Email",                 mandatory: true,  sort: 0, infoG, infoGn);
        AddIfMissing(memberType, textBox,   "firstName",       "First Name",            mandatory: true,  sort: 1, infoG, infoGn);
        AddIfMissing(memberType, textBox,   "lastName",        "Last Name",             mandatory: true,  sort: 2, infoG, infoGn);
        AddIfMissing(memberType, textBox,   "fieldNumber",     "Field Number",          mandatory: true,  sort: 3, infoG, infoGn);
        AddIfMissing(memberType, textBox,   "membershipLevel", "Membership Level",      mandatory: true,  sort: 4, infoG, infoGn);
        // Address (aliases match hallMember for consistency)
        AddIfMissing(memberType, textBox,   "billingAddress",    "Address",             mandatory: true,  sort: 5,  infoG, infoGn);
        AddIfMissing(memberType, textBox,   "addressLine2",      "Address Line 2",      mandatory: false, sort: 6,  infoG, infoGn);
        AddIfMissing(memberType, textBox,   "billingPostalCode", "Postal Code",         mandatory: true,  sort: 7,  infoG, infoGn);
        AddIfMissing(memberType, textBox,   "billingCity",       "City",                mandatory: true,  sort: 8,  infoG, infoGn);
        AddIfMissing(memberType, textBox,   "billingCountry",    "Country",             mandatory: false, sort: 9,  infoG, infoGn);
        AddIfMissing(memberType, textBox,   "phone",             "Phone",               mandatory: false, sort: 10, infoG, infoGn);
        // Floor display
        AddIfMissing(memberType, trueFalse, "showNameOnFloor", "Show Name on Floor",   mandatory: false, sort: 11, infoG, infoGn);
        AddIfMissing(memberType, textBox,   "floorDisplayName", "Floor Display Name",  mandatory: false, sort: 12, infoG, infoGn);
        // Admin
        AddIfMissing(memberType, textBox,   "status",                   "Status",                        mandatory: false, sort: 0, adminG, adminGn);
        AddIfMissing(memberType, textBox,   "paidAt",                   "Paid At (ISO date)",             mandatory: false, sort: 1, adminG, adminGn);
        AddIfMissing(memberType, textBox,   "paidBy",                   "Paid By",                       mandatory: false, sort: 2, adminG, adminGn);
        AddIfMissing(memberType, textBox,   "confirmedAt",              "Confirmed At (ISO date)",        mandatory: false, sort: 3, adminG, adminGn);
        AddIfMissing(memberType, textBox,   "confirmedBy",              "Confirmed By",                  mandatory: false, sort: 4, adminG, adminGn);
        AddIfMissing(memberType, textBox,   "exportedToAccountingAt",   "Exported to Accounting At",     mandatory: false, sort: 5, adminG, adminGn);
        AddIfMissing(memberType, textBox,   "exportedToAccountingBy",   "Exported to Accounting By",     mandatory: false, sort: 6, adminG, adminGn);
        AddIfMissing(memberType, textArea,  "notes",                    "Notes",                         mandatory: false, sort: 7, adminG, adminGn);

        memberTypeService.Save(memberType);
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private void AddIfMissing(IMemberType memberType, IDataType dataType, string alias, string name,
        bool mandatory, int sort, string groupAlias, string groupName)
    {
        if (memberType.PropertyTypeExists(alias)) return;
        memberType.AddPropertyType(Prop(dataType, alias, name, mandatory, sort), groupAlias, groupName);
    }

    private PropertyType Prop(IDataType dataType, string alias, string name, bool mandatory, int sortOrder)
        => new PropertyType(shortStringHelper, dataType, alias)
        {
            Name = name,
            Mandatory = mandatory,
            SortOrder = sortOrder
        };
}
