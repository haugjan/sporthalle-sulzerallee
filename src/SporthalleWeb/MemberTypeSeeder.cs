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
    private const string Alias = "hallMember";

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

        var dateTime = dataTypeService.GetAll()
            .FirstOrDefault(d => d.EditorAlias == "Umbraco.DateTime")
            ?? throw new InvalidOperationException("Umbraco.DateTime data type not found.");

        var memberType = memberTypeService.Get(Alias);
        if (memberType is null)
        {
            memberType = new MemberType(shortStringHelper, -1)
            {
                Alias = Alias,
                Name = "Hall Renter",
                Icon = "icon-user",
                Description = "Mieter der Sporthalle Sulzerallee"
            };
        }

        const string groupAlias = "renterInfo";
        const string groupName = "Renter Info";

        AddIfMissing(memberType, textBox,   "renterType",          "Renter Type",           mandatory: true,  sort: 0,  groupAlias, groupName);
        AddIfMissing(memberType, textBox,   "name",                "Name",                  mandatory: false, sort: 1,  groupAlias, groupName);
        AddIfMissing(memberType, textBox,   "contactFirstName",    "Contact First Name",    mandatory: true,  sort: 2,  groupAlias, groupName);
        AddIfMissing(memberType, textBox,   "contactLastName",     "Contact Last Name",     mandatory: true,  sort: 3,  groupAlias, groupName);
        AddIfMissing(memberType, textBox,   "billingAddress",      "Billing Address",       mandatory: true,  sort: 4,  groupAlias, groupName);
        AddIfMissing(memberType, textBox,   "addressLine2",        "Address Line 2",        mandatory: false, sort: 5,  groupAlias, groupName);
        AddIfMissing(memberType, textBox,   "billingPostalCode",   "Billing Postal Code",   mandatory: true,  sort: 6,  groupAlias, groupName);
        AddIfMissing(memberType, textBox,   "billingCity",         "Billing City",          mandatory: true,  sort: 7,  groupAlias, groupName);
        AddIfMissing(memberType, textBox,   "billingCountry",      "Billing Country",       mandatory: true,  sort: 8,  groupAlias, groupName);
        AddIfMissing(memberType, textBox,   "phone",               "Phone",                 mandatory: false, sort: 9,  groupAlias, groupName);
        AddIfMissing(memberType, trueFalse, "hasKey",              "Has Key",               mandatory: false, sort: 10, groupAlias, groupName);
        AddIfMissing(memberType, textArea,  "notes",               "Notes",                 mandatory: false, sort: 11, groupAlias, groupName);
        AddIfMissing(memberType, dateTime,  "magicLinkSentAt",     "Magic Link Sent At",    mandatory: false, sort: 12, groupAlias, groupName);
        AddIfMissing(memberType, dateTime,  "passwordResetSentAt", "Password Reset Sent At",mandatory: false, sort: 13, groupAlias, groupName);

        memberTypeService.Save(memberType);
        return Task.CompletedTask;
    }

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
