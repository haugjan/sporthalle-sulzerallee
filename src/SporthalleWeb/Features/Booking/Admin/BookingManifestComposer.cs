using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Manifest;
using Umbraco.Cms.Infrastructure.Manifest;

namespace SporthalleWeb.Features.Booking.Admin;

public sealed class BookingManifestComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<IPackageManifestReader, BookingManifestReader>();
    }
}

internal sealed class BookingManifestReader : IPackageManifestReader
{
    public Task<IEnumerable<PackageManifest>> ReadPackageManifestsAsync()
    {
        var manifest = new PackageManifest
        {
            Name = "Booking",
            Version = "1.0.0",
            Extensions =
            [
                new
                {
                    type = "section",
                    alias = "Sporthalle.Booking",
                    name = "Booking",
                    weight = 800,
                    meta = new { label = "Booking", pathname = "booking" }
                },
                new
                {
                    type = "dashboard",
                    alias = "Sporthalle.Booking.Dashboard",
                    name = "Booking Dashboard",
                    element = "/App_Plugins/Booking/booking-view.js",
                    elementName = "booking-view",
                    weight = 100,
                    meta = new { label = "Übersicht", pathname = "overview" },
                    conditions = new object[]
                    {
                        new
                        {
                            alias = "Umb.Condition.SectionAlias",
                            match = "Sporthalle.Booking"
                        }
                    }
                }
            ]
        };

        return Task.FromResult<IEnumerable<PackageManifest>>([manifest]);
    }
}
