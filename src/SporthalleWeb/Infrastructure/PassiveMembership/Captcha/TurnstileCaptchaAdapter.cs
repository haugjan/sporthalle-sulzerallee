using System.Text.Json;
using Microsoft.Extensions.Options;
using SporthalleWeb.Domain.PassiveMembership.Ports;

namespace SporthalleWeb.Infrastructure.PassiveMembership.Captcha;

public class TurnstileCaptchaAdapter(HttpClient http, IOptions<TurnstileOptions> opts) : ICaptchaPort
{
    private readonly TurnstileOptions _opts = opts.Value;

    public async Task<bool> VerifyAsync(string token, string remoteIp)
    {
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["secret"]   = _opts.SecretKey,
            ["response"] = token,
            ["remoteip"] = remoteIp
        });

        var response = await http.PostAsync(
            "https://challenges.cloudflare.com/turnstile/v0/siteverify", form);

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json);
        return result.GetProperty("success").GetBoolean();
    }
}
