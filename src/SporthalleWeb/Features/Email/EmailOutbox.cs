namespace SporthalleWeb.Features.Email;

public enum OutboxStatus
{
    Pending = 0,
    Sending = 1,
    Sent = 2,
    Failed = 3
}

public sealed record OutboxEnqueueRequest(
    string FromEmail,
    string FromName,
    string ToEmail,
    string? ToName,
    string? BccEmail,
    string Subject,
    string HtmlBody,
    string? Source = null,
    string? Reference = null);

public sealed record OutboxMessage(
    int Id,
    string FromEmail,
    string FromName,
    string ToEmail,
    string? ToName,
    string? BccEmail,
    string Subject,
    string HtmlBody,
    int Attempts,
    string? SentVia);

public sealed record OutboxEntry(
    int Id,
    string FromEmail,
    string ToEmail,
    string? ToName,
    string Subject,
    OutboxStatus Status,
    int Attempts,
    string? SentVia,
    string? LastError,
    string? Source,
    string? Reference,
    DateTime CreatedAt,
    DateTime NextAttemptAt,
    DateTime? SentAt);

public interface IEmailOutbox
{
    Task EnqueueAsync(OutboxEnqueueRequest request, CancellationToken cancellationToken = default);
    Task<OutboxMessage?> ClaimNextDueAsync(DateTime now, CancellationToken cancellationToken = default);
    Task MarkSentAsync(int id, string sentVia, DateTime sentAt);
    Task RescheduleAsync(int id, string lastError, DateTime nextAttemptAt);
    Task MarkFailedAsync(int id, string lastError);
    Task<int> PurgeSentBeforeAsync(DateTime cutoff);
}

public interface IOutboxAdminReport
{
    Task<IReadOnlyList<OutboxEntry>> ListAsync(bool includeSent, DateTime sentSince);
    Task<bool> RequeueAsync(int id);
}
