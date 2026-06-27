namespace SporthalleWeb.Features.Booking;

public interface IBookingAudit
{
    Task LogAsync(string entityType, int entityId, string action,
        string changedBy, object? oldState, object? newState,
        string? remoteIp = null, string? notes = null);
}
