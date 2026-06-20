using System.Text.Json;
using NPoco;
using Umbraco.Cms.Infrastructure.Scoping;
using SporthalleWeb.Domain.Reservierung.Ports;
using SporthalleWeb.Infrastructure.Reservierung.Persistence.DbRecords;

namespace SporthalleWeb.Infrastructure.Reservierung.Persistence;

public sealed class BookingAuditRepository(IScopeProvider scopeProvider) : IBookingAuditRepository
{
    public async Task LogAsync(
        string entityType, int entityId, string action,
        string changedBy, object? oldState, object? newState,
        string? remoteIp = null, string? notes = null)
    {
        using var scope = scopeProvider.CreateScope();
        var record = new BookingAuditLogRecord
        {
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            ChangedBy = changedBy,
            ChangedAt = DateTime.UtcNow,
            OldStatusJson = oldState is not null ? JsonSerializer.Serialize(oldState) : null,
            NewStatusJson = newState is not null ? JsonSerializer.Serialize(newState) : null,
            RemoteIp = remoteIp,
            Notes = notes
        };
        await scope.Database.InsertAsync(record);
        scope.Complete();
    }
}
