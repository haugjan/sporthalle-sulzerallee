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
        if (memberTypeService.Get(Alias) != null)
            return Task.CompletedTask;

        var textBox = dataTypeService.GetAll()
            .FirstOrDefault(d => d.EditorAlias == "Umbraco.TextBox")
            ?? throw new InvalidOperationException("Umbraco.TextBox data type not found.");

        var trueFalse = dataTypeService.GetAll()
            .FirstOrDefault(d => d.EditorAlias == "Umbraco.TrueFalse")
            ?? throw new InvalidOperationException("Umbraco.TrueFalse data type not found.");

        var dateTime = dataTypeService.GetAll()
            .FirstOrDefault(d => d.EditorAlias == "Umbraco.DateTime")
            ?? throw new InvalidOperationException("Umbraco.DateTime data type not found.");

        var memberType = new MemberType(shortStringHelper, -1)
        {
            Alias = Alias,
            Name = "Hall Renter",
            Icon = "icon-user",
            Description = "Mieter der Sporthalle Sulzerallee"
        };

        const string groupAlias = "renterInfo";
        const string groupName = "Renter Info";

        int sort = 0;
        memberType.AddPropertyType(Prop(textBox, "renterType",           "Renter Type",           true,  sort++), groupAlias, groupName);
        memberType.AddPropertyType(Prop(textBox, "billingName",          "Billing Name",          true,  sort++), groupAlias, groupName);
        memberType.AddPropertyType(Prop(textBox, "billingAddress",       "Billing Address",       true,  sort++), groupAlias, groupName);
        memberType.AddPropertyType(Prop(textBox, "billingPostalCode",    "Billing Postal Code",   true,  sort++), groupAlias, groupName);
        memberType.AddPropertyType(Prop(textBox, "billingCity",          "Billing City",          true,  sort++), groupAlias, groupName);
        memberType.AddPropertyType(Prop(textBox, "billingCountry",       "Billing Country",       true,  sort++), groupAlias, groupName);
        memberType.AddPropertyType(Prop(textBox, "phone",                "Phone",                 false, sort++), groupAlias, groupName);
        memberType.AddPropertyType(Prop(trueFalse, "hasKey",             "Has Key",               false, sort++), groupAlias, groupName);
        memberType.AddPropertyType(Prop(dateTime, "magicLinkSentAt",     "Magic Link Sent At",    false, sort++), groupAlias, groupName);
        memberType.AddPropertyType(Prop(dateTime, "passwordResetSentAt", "Password Reset Sent At",false, sort),   groupAlias, groupName);

        memberTypeService.Save(memberType);
        return Task.CompletedTask;
    }

    private PropertyType Prop(IDataType dataType, string alias, string name, bool mandatory, int sortOrder)
        => new PropertyType(shortStringHelper, dataType, alias)
        {
            Name = name,
            Mandatory = mandatory,
            SortOrder = sortOrder
        };
}
