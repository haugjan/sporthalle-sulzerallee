using SporthalleWeb.Domain.Reservierung.Ports;
using Umbraco.Cms.Core.Services;

namespace SporthalleWeb.Infrastructure.Reservierung.Config;

public sealed class UmbracoHallConfigurationAdapter(IContentService contentService) : IHallConfigurationPort
{
    private const string ConfigAlias = "reservierungKonfiguration";

    private Umbraco.Cms.Core.Models.IContent? GetConfigNode() =>
        contentService.GetRootContent().FirstOrDefault(c => c.ContentType.Alias == ConfigAlias);

    public Task<decimal> GetPricePerBlockAsync() =>
        Task.FromResult(GetConfigNode()?.GetValue<decimal>("pricePerBlock") ?? 50m);

    public Task<int> GetBlockDurationMinutesAsync() =>
        Task.FromResult(GetConfigNode()?.GetValue<int>("blockDurationMinutes") is int v && v > 0 ? v : 60);

    public Task<int> GetOpeningHourStartAsync() =>
        Task.FromResult(GetConfigNode()?.GetValue<int>("openingHourStart") is int v && v > 0 ? v : 8);

    public Task<int> GetOpeningHourEndAsync() =>
        Task.FromResult(GetConfigNode()?.GetValue<int>("openingHourEnd") is int v && v > 0 ? v : 22);

    public Task<int> GetMaxWeeksAheadAsync() =>
        Task.FromResult(GetConfigNode()?.GetValue<int>("maxWeeksAhead") is int v && v > 0 ? v : 12);

    public Task<IReadOnlyList<int>> GetBuchbareDauernAsync()
    {
        var raw = GetConfigNode()?.GetValue<string>("buchbareDauern") ?? "60,120,180";
        var result = raw.Split(',')
            .Select(s => int.TryParse(s.Trim(), out var i) ? i : 0)
            .Where(i => i > 0)
            .ToList();
        return Task.FromResult<IReadOnlyList<int>>(result.Count > 0 ? result : [60, 120]);
    }

    public Task<IReadOnlyList<string>> GetAnlasseAsync()
    {
        var raw = GetConfigNode()?.GetValue<string>("anlaesse") ?? "Sport,Training,Wettkampf,Schule,Sonstiges";
        var result = raw.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
        return Task.FromResult<IReadOnlyList<string>>(result);
    }
}
