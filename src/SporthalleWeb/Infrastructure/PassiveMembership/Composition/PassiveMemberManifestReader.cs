using Umbraco.Cms.Core.Manifest;
using Umbraco.Cms.Infrastructure.Manifest;

namespace SporthalleWeb.Infrastructure.PassiveMembership.Composition;

internal sealed class PassiveMemberManifestReader : IPackageManifestReader
{
    private static readonly PackageManifest Manifest = new()
    {
        Name = "PassivMitglieder",
        Version = "1.0.0",
        AllowTelemetry = false,
        Extensions =
        [
            new
            {
                type = "section",
                alias = "pm.Section",
                name = "Passivmitglieder",
                weight = 900,
                meta = new { label = "Passivmitglieder", pathname = "passivmitglieder" }
            },
            new
            {
                type = "dashboard",
                alias = "pm.Dashboard",
                name = "Passivmitglieder",
                element = "/App_Plugins/PassivMitglieder/pm-entrypoint.js",
                elementName = "pm-admin",
                weight = 100,
                conditions = new object[]
                {
                    new { alias = "Umb.Condition.SectionAlias", match = "pm.Section" }
                },
                meta = new { label = "Passivmitglieder", pathname = "passivmitglieder" }
            }
        ]
    };

    public Task<IEnumerable<PackageManifest>> ReadPackageManifestsAsync()
        => Task.FromResult<IEnumerable<PackageManifest>>([Manifest]);
}
