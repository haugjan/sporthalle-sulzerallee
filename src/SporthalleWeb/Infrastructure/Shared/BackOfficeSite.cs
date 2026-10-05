using Microsoft.AspNetCore.Http;

namespace SporthalleWeb.Infrastructure.Shared;

public static class BackOfficeSite
{
    public const string Path = "/umbraco";

    public static string? AdminHostFor(string host) => host.ToLowerInvariant() switch
    {
        "sporthalle-sulzerallee.ch" or "www.sporthalle-sulzerallee.ch" => "admin.sporthalle-sulzerallee.ch",
        "www-dev.sporthalle-sulzerallee.ch" => "admin-dev.sporthalle-sulzerallee.ch",
        _ => null
    };

    public static string UrlFor(HttpRequest request)
    {
        var adminHost = AdminHostFor(request.Host.Host);
        if (adminHost is null)
            return Path;

        var proto = request.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? request.Scheme;
        return $"{proto}://{adminHost}{Path}";
    }
}
