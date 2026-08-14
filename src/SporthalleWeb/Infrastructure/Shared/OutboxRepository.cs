using NPoco;
using SporthalleWeb.Features.Email;
using Umbraco.Cms.Infrastructure.Scoping;

namespace SporthalleWeb.Infrastructure.Shared;

public sealed class OutboxRepository(IScopeProvider scopeProvider, OutboxSignal signal)
    : IEmailOutbox, IOutboxAdminReport
{
    public async Task EnqueueAsync(OutboxEnqueueRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        using var scope = scopeProvider.CreateScope();
        await scope.Database.InsertAsync(new OutboxEmailRecord
        {
            FromEmail = request.FromEmail,
            FromName = request.FromName,
            ToEmail = request.ToEmail,
            ToName = request.ToName,
            BccEmail = request.BccEmail,
            Subject = request.Subject,
            BodyHtml = request.HtmlBody,
            Status = (int)OutboxStatus.Pending,
            Attempts = 0,
            SentVia = null,
            LastError = null,
            Source = request.Source,
            Reference = request.Reference,
            CreatedAt = now,
            NextAttemptAt = now,
            SentAt = null
        });
        scope.Complete();
        signal.Notify();
    }

    public async Task<OutboxMessage?> ClaimNextDueAsync(DateTime now, CancellationToken cancellationToken = default)
    {
        using var scope = scopeProvider.CreateScope();
        var isSqlite = scope.Database.DatabaseType.GetType().Name.Contains("SQLite", StringComparison.OrdinalIgnoreCase);

        OutboxEmailRecord? row;

        if (isSqlite)
        {
            var ids = await scope.Database.FetchAsync<int>(
                new Sql("SELECT Id FROM OutboxEmails WHERE Status = 0 AND NextAttemptAt <= @0 ORDER BY NextAttemptAt, Id LIMIT 1", now));
            var nextId = ids.FirstOrDefault();
            if (nextId == 0) { scope.Complete(); return null; }

            var affected = await scope.Database.ExecuteAsync(
                new Sql("UPDATE OutboxEmails SET Status = 1, Attempts = Attempts + 1 WHERE Id = @0 AND Status = 0", nextId));
            if (affected == 0) { scope.Complete(); return null; }

            var rows = await scope.Database.FetchAsync<OutboxEmailRecord>(
                new Sql("SELECT * FROM OutboxEmails WHERE Id = @0", nextId));
            row = rows.FirstOrDefault();
        }
        else
        {
            var claimed = await scope.Database.FetchAsync<OutboxEmailRecord>(
                new Sql(
                    ";WITH due AS (SELECT TOP(1) Id FROM OutboxEmails WITH (READPAST, UPDLOCK, ROWLOCK) " +
                    "WHERE Status = 0 AND NextAttemptAt <= @0 ORDER BY NextAttemptAt, Id) " +
                    "UPDATE o SET Status = 1, Attempts = o.Attempts + 1 " +
                    "OUTPUT inserted.Id, inserted.FromEmail, inserted.FromName, inserted.ToEmail, inserted.ToName, " +
                    "inserted.BccEmail, inserted.Subject, inserted.BodyHtml, inserted.Status, inserted.Attempts, " +
                    "inserted.SentVia, inserted.LastError, inserted.Source, inserted.Reference, " +
                    "inserted.CreatedAt, inserted.NextAttemptAt, inserted.SentAt " +
                    "FROM OutboxEmails o INNER JOIN due ON o.Id = due.Id", now));
            row = claimed.FirstOrDefault();
        }

        scope.Complete();
        return row is null ? null : ToMessage(row);
    }

    public async Task MarkSentAsync(int id, string sentVia, DateTime sentAt)
    {
        using var scope = scopeProvider.CreateScope();
        await scope.Database.ExecuteAsync(
            new Sql("UPDATE OutboxEmails SET Status = 2, SentVia = @1, SentAt = @2, LastError = NULL WHERE Id = @0",
                id, sentVia, sentAt));
        scope.Complete();
    }

    public async Task RescheduleAsync(int id, string lastError, DateTime nextAttemptAt)
    {
        using var scope = scopeProvider.CreateScope();
        await scope.Database.ExecuteAsync(
            new Sql("UPDATE OutboxEmails SET Status = 0, LastError = @1, NextAttemptAt = @2 WHERE Id = @0",
                id, Trim(lastError), nextAttemptAt));
        scope.Complete();
    }

    public async Task MarkFailedAsync(int id, string lastError)
    {
        using var scope = scopeProvider.CreateScope();
        await scope.Database.ExecuteAsync(
            new Sql("UPDATE OutboxEmails SET Status = 3, LastError = @1 WHERE Id = @0",
                id, Trim(lastError)));
        scope.Complete();
    }

    public async Task<int> PurgeSentBeforeAsync(DateTime cutoff)
    {
        using var scope = scopeProvider.CreateScope();
        var removed = await scope.Database.ExecuteAsync(
            new Sql("DELETE FROM OutboxEmails WHERE Status = 2 AND SentAt IS NOT NULL AND SentAt < @0", cutoff));
        scope.Complete();
        return removed;
    }

    public async Task<IReadOnlyList<OutboxEntry>> ListAsync(bool includeSent, DateTime sentSince)
    {
        using var scope = scopeProvider.CreateScope();
        Sql sql = includeSent
            ? new Sql("SELECT * FROM OutboxEmails WHERE Status <> 2 OR (Status = 2 AND SentAt >= @0) ORDER BY CreatedAt DESC", sentSince)
            : new Sql("SELECT * FROM OutboxEmails WHERE Status <> 2 ORDER BY CreatedAt DESC");
        var rows = await scope.Database.FetchAsync<OutboxEmailRecord>(sql);
        scope.Complete();
        return rows.Select(r => new OutboxEntry(
            r.Id, r.FromEmail, r.ToEmail, r.ToName, r.Subject,
            (OutboxStatus)r.Status, r.Attempts, r.SentVia, r.LastError,
            r.Source, r.Reference, r.CreatedAt, r.NextAttemptAt, r.SentAt)).ToList();
    }

    public async Task<bool> RequeueAsync(int id)
    {
        using var scope = scopeProvider.CreateScope();
        var affected = await scope.Database.ExecuteAsync(
            new Sql("UPDATE OutboxEmails SET Status = 0, NextAttemptAt = @1, LastError = NULL WHERE Id = @0 AND Status <> 2",
                id, DateTime.UtcNow));
        scope.Complete();
        if (affected > 0) signal.Notify();
        return affected > 0;
    }

    private static OutboxMessage ToMessage(OutboxEmailRecord r) => new(
        r.Id, r.FromEmail, r.FromName ?? "", r.ToEmail, r.ToName, r.BccEmail,
        r.Subject, r.BodyHtml, r.Attempts, r.SentVia);

    private static string Trim(string value) => value.Length > 1000 ? value[..1000] : value;
}
