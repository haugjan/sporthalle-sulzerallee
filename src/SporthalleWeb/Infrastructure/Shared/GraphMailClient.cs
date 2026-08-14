using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SporthalleWeb.Infrastructure.Shared;

public sealed class GraphMailClient(
    IHttpClientFactory httpClientFactory,
    IConfiguration config,
    ILogger<GraphMailClient> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
        { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    private static readonly SemaphoreSlim TokenLock = new(1, 1);
    private static string? _cachedToken;
    private static string? _cachedForClient;
    private static DateTimeOffset _cachedUntil;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(config["Graph:TenantId"])
        && !string.IsNullOrWhiteSpace(config["Graph:ClientId"])
        && !string.IsNullOrWhiteSpace(config["Graph:ClientSecret"]);

    public async Task<bool> SendAsync(
        string fromEmail, string fromName,
        string toEmail, string toName,
        string subject, string htmlBody,
        string? bccEmail = null,
        CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            logger.LogWarning("Graph mail not configured — skipping email to {Email}", toEmail);
            return false;
        }

        var tenantId = config["Graph:TenantId"]!;
        var clientId = config["Graph:ClientId"]!;
        var clientSecret = config["Graph:ClientSecret"]!;

        var token = await GetTokenAsync(tenantId, clientId, clientSecret, ct);
        if (token is null)
        {
            logger.LogError("Graph token acquisition failed — skipping email to {Email}", toEmail);
            return false;
        }

        var payload = new GraphSendMail(
            new GraphMessage(
                subject,
                new GraphBody("HTML", htmlBody),
                new[] { new GraphRecipient(new GraphEmailAddress(toName, toEmail)) },
                bccEmail is not null
                    ? new[] { new GraphRecipient(new GraphEmailAddress(bccEmail, bccEmail)) }
                    : null),
            SaveToSentItems: false);

        try
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var json = JsonSerializer.Serialize(payload, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var url = $"https://graph.microsoft.com/v1.0/users/{Uri.EscapeDataString(fromEmail)}/sendMail";
            var response = await client.PostAsync(url, content, ct);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Graph email sent to {Recipient} from {Sender}", toEmail, fromEmail);
                return true;
            }

            var error = await response.Content.ReadAsStringAsync(ct);
            logger.LogError("Graph email to {Recipient} failed: {Status} {Error}", toEmail, (int)response.StatusCode, error);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Graph email to {Recipient} threw", toEmail);
            return false;
        }
    }

    private async Task<string?> GetTokenAsync(string tenantId, string clientId, string clientSecret, CancellationToken ct)
    {
        await TokenLock.WaitAsync(ct);
        try
        {
            if (_cachedToken is not null && _cachedForClient == clientId && _cachedUntil > DateTimeOffset.UtcNow.AddMinutes(2))
                return _cachedToken;

            var client = httpClientFactory.CreateClient();
            var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["scope"] = "https://graph.microsoft.com/.default",
                ["grant_type"] = "client_credentials"
            });
            var response = await client.PostAsync(
                $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token", form, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Graph token request failed: {Status} {Error}", (int)response.StatusCode, body);
                return null;
            }

            using var doc = JsonDocument.Parse(body);
            var token = doc.RootElement.GetProperty("access_token").GetString();
            var expiresIn = doc.RootElement.TryGetProperty("expires_in", out var e) ? e.GetInt32() : 3600;
            _cachedToken = token;
            _cachedForClient = clientId;
            _cachedUntil = DateTimeOffset.UtcNow.AddSeconds(expiresIn);
            return token;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Graph token acquisition threw");
            return null;
        }
        finally { TokenLock.Release(); }
    }

    private sealed record GraphSendMail(
        [property: JsonPropertyName("message")] GraphMessage Message,
        [property: JsonPropertyName("saveToSentItems")] bool SaveToSentItems);

    private sealed record GraphMessage(
        [property: JsonPropertyName("subject")] string Subject,
        [property: JsonPropertyName("body")] GraphBody Body,
        [property: JsonPropertyName("toRecipients")] GraphRecipient[] ToRecipients,
        [property: JsonPropertyName("bccRecipients")] GraphRecipient[]? BccRecipients);

    private sealed record GraphBody(
        [property: JsonPropertyName("contentType")] string ContentType,
        [property: JsonPropertyName("content")] string Content);

    private sealed record GraphRecipient(
        [property: JsonPropertyName("emailAddress")] GraphEmailAddress EmailAddress);

    private sealed record GraphEmailAddress(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("address")] string Address);
}
