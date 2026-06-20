namespace SporthalleWeb.Domain.Reservierung.Ports;

public interface IBookingAuditRepository
{
    Task LogAsync(string entityType, int entityId, string action,
        string changedBy, object? oldState, object? newState,
        string? remoteIp = null, string? notes = null);
}
