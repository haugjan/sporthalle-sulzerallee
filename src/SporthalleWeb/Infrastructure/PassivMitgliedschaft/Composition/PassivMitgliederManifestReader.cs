using System.Text.Json;
using Umbraco.Cms.Core.Manifest;
using Umbraco.Cms.Infrastructure.Manifest;

namespace SporthalleWeb.Infrastructure.PassivMitgliedschaft.Composition;

internal sealed class PassivMitgliederManifestReader : IPackageManifestReader
{
    private static readonly PackageManifest Manifest = new()
    {
        Name = "PassivMitglieder",
        Version = "1.0.0",
        AllowTelemetry = false,
        Extensions = JsonSerializer.Deserialize<object[]>("""
            [
              {
                "type": "backofficeEntryPoint",
                "alias": "pm.EntryPoint",
                "name": "PassivMitglieder",
                "js": "/App_Plugins/PassivMitglieder/pm-entrypoint.js"
              }
            ]
            """)!,
    };

    public Task<IEnumerable<PackageManifest>> ReadPackageManifestsAsync()
        => Task.FromResult<IEnumerable<PackageManifest>>([Manifest]);
}
