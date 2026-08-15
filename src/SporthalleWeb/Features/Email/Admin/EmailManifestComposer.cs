using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Manifest;
using Umbraco.Cms.Infrastructure.Manifest;

namespace SporthalleWeb.Features.Email.Admin;

public sealed class EmailManifestComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<IPackageManifestReader, EmailManifestReader>();
    }
}

internal sealed class EmailManifestReader : IPackageManifestReader
{
    private static readonly PackageManifest Manifest = new()
    {
        Name = "SporthalleEmails",
        Version = "1.0.0",
        AllowTelemetry = false,
        Extensions =
        [
            new
            {
                type = "section",
                alias = "Sporthalle.Emails",
                name = "E-Mails",
                weight = 700,
                meta = new { label = "E-Mails", pathname = "emails" }
            },
            new
            {
                type = "dashboard",
                alias = "Sporthalle.Emails.Dashboard",
                name = "E-Mails",
                element = "/App_Plugins/Emails/email-admin.js",
                elementName = "email-admin",
                weight = 100,
                conditions = new object[]
                {
                    new { alias = "Umb.Condition.SectionAlias", match = "Sporthalle.Emails" }
                },
                meta = new { label = "E-Mails", pathname = "emails" }
            }
        ]
    };

    public Task<IEnumerable<PackageManifest>> ReadPackageManifestsAsync()
        => Task.FromResult<IEnumerable<PackageManifest>>([Manifest]);
}
