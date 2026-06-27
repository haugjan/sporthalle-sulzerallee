using System.Text.Json;

namespace SporthalleWeb.Infrastructure.Shared;

internal static class UmbracoDropdownHelper
{
    /// <summary>
    /// Umbraco's FlexDropdown editor persists values as a JSON array (e.g. ["Privatperson"]).
    /// This method extracts the first element so domain code receives a plain string.
    /// </summary>
    internal static string ParseDropdownValue(string? raw, string? fallback)
    {
        if (raw is null) return fallback;
        if (raw.StartsWith('['))
        {
            try
            {
                var items = JsonSerializer.Deserialize<string[]>(raw);
                return items?.FirstOrDefault() ?? fallback;
            }
            catch { return fallback; }
        }
        return raw;
    }
}
