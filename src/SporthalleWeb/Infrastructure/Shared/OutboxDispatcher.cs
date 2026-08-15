using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SporthalleWeb.Features.Email;

namespace SporthalleWeb.Infrastructure.Shared;

public sealed class OutboxDispatcher(
    IServiceScopeFactory scopeFactory,
    OutboxSignal signal,
    GraphMailClient graph,
    IConfiguration config,
    ILogger<OutboxDispatcher> logger) : BackgroundService
{
    private const int MaxAttempts = 6;
    private static readonly TimeSpan Pace = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan FallbackPoll = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan PurgeInterval = TimeSpan.FromHours(1);

    private DateTime _lastPurge = DateTime.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogSecretExpiryIfNear();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PurgeIfDueAsync();

                using var scope = scopeFactory.CreateScope();
                var outbox = scope.ServiceProvider.GetRequiredService<IEmailOutbox>();

                var message = await outbox.ClaimNextDueAsync(DateTime.UtcNow, stoppingToken);
                if (message is null)
                {
                    await signal.WaitAsync(FallbackPoll, stoppingToken);
                    continue;
                }

                await DeliverAsync(outbox, message, stoppingToken);
                await Task.Delay(Pace, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex) when (IsTableNotFound(ex))
            {
                logger.LogWarning("Outbox table not ready yet, retrying in 10s.");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox dispatcher loop error.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task DeliverAsync(IEmailOutbox outbox, OutboxMessage message, CancellationToken ct)
    {
        var error = await graph.SendAsync(
            message.FromEmail, message.FromName,
            message.ToEmail, message.ToName ?? message.ToEmail,
            message.Subject, message.HtmlBody,
            message.BccEmail, ct);

        if (error is null)
        {
            await outbox.MarkSentAsync(message.Id, "Graph", DateTime.UtcNow);
        }
        else if (message.Attempts >= MaxAttempts)
        {
            logger.LogError("Outbox message {Id} failed permanently after {Attempts} attempts: {Error}",
                message.Id, message.Attempts, error);
            await outbox.MarkFailedAsync(message.Id, error);
        }
        else
        {
            logger.LogWarning("Outbox message {Id} failed (attempt {Attempts}), rescheduling: {Error}",
                message.Id, message.Attempts, error);
            await outbox.RescheduleAsync(message.Id, error, DateTime.UtcNow + BackoffFor(message.Attempts));
        }
    }

    private async Task PurgeIfDueAsync()
    {
        var now = DateTime.UtcNow;
        if (now - _lastPurge < PurgeInterval) return;
        _lastPurge = now;

        var retentionDays = int.TryParse(config["Email:Outbox:RetentionDays"], out var days) && days > 0 ? days : 30;

        try
        {
            using var scope = scopeFactory.CreateScope();
            var outbox = scope.ServiceProvider.GetRequiredService<IEmailOutbox>();
            var removed = await outbox.PurgeSentBeforeAsync(now.AddDays(-retentionDays));
            if (removed > 0) logger.LogInformation("Outbox purge removed {Count} sent e-mails.", removed);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Outbox purge failed.");
        }
    }

    private void LogSecretExpiryIfNear()
    {
        var raw = config["Graph:ClientSecretExpires"];
        if (!DateTime.TryParse(raw, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var expires)) return;

        var days = (int)Math.Floor((expires.Date - DateTime.UtcNow.Date).TotalDays);
        if (days < 0)
            logger.LogError("Graph client secret expired on {Date:yyyy-MM-dd}. E-mail sending is broken until renewed in Entra.", expires);
        else if (days <= 30)
            logger.LogWarning("Graph client secret expires on {Date:yyyy-MM-dd} (in {Days} days). Renew it in Entra.", expires, days);
    }

    private static TimeSpan BackoffFor(int attempts)
    {
        var seconds = Math.Min(900d, 60d * Math.Pow(2, Math.Max(0, attempts - 1)));
        return TimeSpan.FromSeconds(seconds);
    }

    private static bool IsTableNotFound(Exception ex)
    {
        var msg = ex.Message;
        return msg.Contains("OutboxEmails", StringComparison.OrdinalIgnoreCase) &&
               (msg.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("no such table", StringComparison.OrdinalIgnoreCase));
    }
}
