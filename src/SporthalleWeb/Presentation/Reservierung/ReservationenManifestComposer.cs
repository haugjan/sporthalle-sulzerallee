using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Manifest;
using Umbraco.Cms.Infrastructure.Manifest;

namespace SporthalleWeb.Presentation.Reservierung;

public sealed class ReservationenManifestComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<IPackageManifestReader, ReservationenManifestReader>();
    }
}

internal sealed class ReservationenManifestReader : IPackageManifestReader
{
    public Task<IEnumerable<PackageManifest>> ReadPackageManifestsAsync()
    {
        var manifest = new PackageManifest
        {
            Name = "Reservationen",
            Version = "1.0.0",
            Extensions =
            [
                new
                {
                    type = "section",
                    alias = "Sporthalle.Reservationen",
                    name = "Reservationen",
                    weight = 110,
                    meta = new { label = "Reservationen", pathname = "reservationen" }
                },
                new
                {
                    type = "dashboard",
                    alias = "Sporthalle.Reservationen.Dashboard",
                    name = "Reservationen Dashboard",
                    element = "/App_Plugins/Reservationen/reservationen-view.js",
                    elementName = "reservationen-view",
                    weight = 100,
                    meta = new { label = "Übersicht", pathname = "overview" },
                    conditions = new object[]
                    {
                        new
                        {
                            alias = "Umb.Condition.SectionAlias",
                            match = "Sporthalle.Reservationen"
                        }
                    }
                }
            ]
        };

        return Task.FromResult<IEnumerable<PackageManifest>>([manifest]);
    }
}
