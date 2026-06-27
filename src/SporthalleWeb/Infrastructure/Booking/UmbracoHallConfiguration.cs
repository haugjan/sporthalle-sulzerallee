using Umbraco.Cms.Core.Services;
using SporthalleWeb.Domain.Booking;
using SporthalleWeb.Features.Booking.Ports;

namespace SporthalleWeb.Infrastructure.Booking;

public sealed class UmbracoHallConfiguration(
    IContentService contentService,
    IHallConfigStore hallConfigService) : IHallConfiguration
{
    private const string ConfigAlias = "reservierungKonfiguration";

    private Umbraco.Cms.Core.Models.IContent? GetConfigNode() =>
        contentService.GetRootContent().FirstOrDefault(c => c.ContentType.Alias == ConfigAlias);

    public Task<int> GetBlockDurationMinutesAsync() =>
        Task.FromResult(GetConfigNode()?.GetValue<int>("blockDurationMinutes") is int v && v > 0 ? v : 60);

    public async Task<int> GetOpeningHourStartAsync()
    {
        var raw = await hallConfigService.GetAsync("kalender_beginn");
        if (int.TryParse(raw, out var h) && h >= 0 && h <= 23) return h;
        return 7;
    }

    public async Task<int> GetOpeningHourEndAsync()
    {
        var raw = await hallConfigService.GetAsync("kalender_ende");
        if (int.TryParse(raw, out var h) && h >= 0 && h <= 23) return h;
        return 23;
    }

    public async Task<int> GetShortNoticeDaysAsync()
    {
        var raw = await hallConfigService.GetAsync("vorlaufzeit_tage");
        if (int.TryParse(raw, out var days) && days >= 0) return days;
        return 3;
    }

    public async Task<DateOnly?> GetBookingCutoffDateAsync()
    {
        var raw = await hallConfigService.GetAsync("buchungs_cutoff_datum");
        if (DateOnly.TryParse(raw, out var date)) return date;
        return null;
    }

    public Task<IReadOnlyList<int>> GetBookableDurationsAsync()
    {
        var raw = GetConfigNode()?.GetValue<string>("buchbareDauern") ?? "60,120,180";
        var result = raw.Split(',')
            .Select(s => int.TryParse(s.Trim(), out var i) ? i : 0)
            .Where(i => i > 0)
            .ToList();
        return Task.FromResult<IReadOnlyList<int>>(result.Count > 0 ? result : [60, 120]);
    }

    public Task<IReadOnlyList<string>> GetEventTypesAsync()
    {
        var raw = GetConfigNode()?.GetValue<string>("anlaesse") ?? "Sport,Training,Wettkampf,Schule,Sonstiges";
        var result = raw.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
        return Task.FromResult<IReadOnlyList<string>>(result);
    }

    public Task<string?> GetPriceTextAsync() =>
        Task.FromResult(GetConfigNode()?.GetValue<string>("preisText"));
}
