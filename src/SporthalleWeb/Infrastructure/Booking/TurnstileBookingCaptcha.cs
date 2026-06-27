using System.Text.Json;
using SporthalleWeb.Features.Booking;


using SporthalleWeb.Domain.Booking;
using SporthalleWeb.Features.Booking.Ports;

namespace SporthalleWeb.Infrastructure.Booking;

public sealed class TurnstileBookingCaptcha(IHttpClientFactory httpClientFactory, IConfiguration config) : ICaptcha
{
    private const string VerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

    public async Task<bool> VerifyAsync(string? token, string? remoteIp)
    {
        var secret = config["Turnstile:SecretKey"];
        if (string.IsNullOrEmpty(secret)) return true;
        if (string.IsNullOrEmpty(token)) return false;

        var client = httpClientFactory.CreateClient("Turnstile");
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["secret"] = secret,
            ["response"] = token,
            ["remoteip"] = remoteIp ?? string.Empty
        });

        var response = await client.PostAsync(VerifyUrl, content);
        if (!response.IsSuccessStatusCode) return false;

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("success", out var success) && success.GetBoolean();
    }
}
