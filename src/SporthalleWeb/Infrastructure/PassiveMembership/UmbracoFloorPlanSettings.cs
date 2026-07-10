using System.Text.Json;
using System.Text.RegularExpressions;
using SporthalleWeb.Domain.PassiveMembership.PassiveMemberAggregate;
using SporthalleWeb.Features.PassiveMembership.Registration;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace SporthalleWeb.Infrastructure.PassiveMembership;

public sealed partial class UmbracoFloorPlanSettings(
    IUmbracoContextFactory umbracoContextFactory,
    IDocumentNavigationQueryService navigationQueryService,
    ILogger<UmbracoFloorPlanSettings> logger)
    : IFloorPlanSettings
{
    private const string ElementAlias = "passivmitgliedschaftElement";

    public async Task<FloorPlanSettings> GetAsync()
    {
        try
        {
            var element = await FindElementAsync();
            if (element is null)
                return FloorPlanSettings.Default;

            var bgUrl = SanitizeUrl(element.Value<IPublishedContent>("bodenplanBild")?.Url());
            var lineColor = SanitizeColor(element.Value<string>("linienFarbe"));
            var (region, special) = ParseRaster(RawRaster(element));

            return new FloorPlanSettings(bgUrl, lineColor, region, special);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Reading floor plan settings failed; using defaults.");
            return FloorPlanSettings.Default;
        }
    }

    public async Task<string?> GetRawRasterAsync()
    {
        try
        {
            var element = await FindElementAsync();
            return element is null ? null : RawRaster(element);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Reading raw raster failed.");
            return null;
        }
    }

    private static string? RawRaster(IPublishedElement element) =>
        element.GetProperty("bodenplanRaster")?.GetSourceValue()?.ToString()
        ?? element.Value<string>("bodenplanRaster");

    private async Task<IPublishedElement?> FindElementAsync()
    {
        using var reference = umbracoContextFactory.EnsureUmbracoContext();
        var content = reference.UmbracoContext.Content;
        if (content is null) return null;

        if (!navigationQueryService.TryGetRootKeys(out var rootKeys)) return null;

        foreach (var rootKey in rootKeys)
        {
            if (!navigationQueryService.TryGetDescendantsKeysOrSelfKeys(rootKey, out var keys))
                keys = [rootKey];

            foreach (var key in keys)
            {
                var node = await content.GetByIdAsync(key, false);
                var blocks = node?.Value<BlockListModel>("content");
                if (blocks is null) continue;
                foreach (var item in blocks)
                {
                    if (string.Equals(item.Content.ContentType.Alias, ElementAlias, StringComparison.Ordinal))
                        return item.Content;
                }
            }
        }
        return null;
    }

    private (GridRegion, SpecialFieldMap) ParseRaster(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return (GridRegion.Default, VipField.Default);

        try
        {
            using var doc = JsonDocument.Parse(json.Trim());
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.String)
            {
                var inner = root.GetString();
                return string.IsNullOrWhiteSpace(inner)
                    ? (GridRegion.Default, VipField.Default)
                    : ParseRaster(inner);
            }

            var region = GridRegion.Default;
            if (root.TryGetProperty("region", out var r) && r.ValueKind == JsonValueKind.Object)
            {
                double Get(string name, double fallback) =>
                    r.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : fallback;
                region = GridRegion.Create(
                    Get("x0", GridRegion.Default.X0),
                    Get("y0", GridRegion.Default.Y0),
                    Get("x1", GridRegion.Default.X1),
                    Get("y1", GridRegion.Default.Y1));
            }

            SpecialFieldMap special;
            if (root.TryGetProperty("special", out var s) && s.ValueKind == JsonValueKind.Array)
            {
                var areas = new List<SpecialArea>();
                foreach (var e in s.EnumerateArray())
                {
                    if (e.ValueKind != JsonValueKind.Object) continue;
                    if (!e.TryGetProperty("from", out var f) || f.ValueKind != JsonValueKind.Number) continue;
                    var from = f.GetInt32();
                    var to = e.TryGetProperty("to", out var t) && t.ValueKind == JsonValueKind.Number ? t.GetInt32() : from;
                    var label = e.TryGetProperty("label", out var l) && l.ValueKind == JsonValueKind.String
                        ? l.GetString() : null;
                    if (string.IsNullOrWhiteSpace(label)) label = "Spezialfeld";
                    areas.Add(new SpecialArea(label, from, to));
                }
                special = new SpecialFieldMap(areas);
            }
            else
            {
                special = VipField.Default;
            }

            return (region, special);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Parsing floor plan raster JSON failed; using defaults.");
            return (GridRegion.Default, VipField.Default);
        }
    }

    private static string? SanitizeUrl(string? value) =>
        value is { Length: > 0 } v && UrlPattern().IsMatch(v) ? v : null;

    private static string? SanitizeColor(string? value) =>
        value is { Length: > 0 } v && ColorPattern().IsMatch(v.Trim()) ? v.Trim() : null;

    [GeneratedRegex("^(/|https?://)[^\\s\"'<>]+$")]
    private static partial Regex UrlPattern();

    [GeneratedRegex("^(#[0-9a-fA-F]{3,8}|rgba?\\([0-9.,%\\s]+\\))$")]
    private static partial Regex ColorPattern();
}
