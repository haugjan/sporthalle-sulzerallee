namespace SporthalleWeb.Domain.Reservierung;

public enum SlotType
{
    Blocker,   // admin-only placeholder, no member required, not shown publicly
    Reserved,  // unconfirmed booking request, member required
    Booked,    // confirmed booking, member required
    Rejected,  // soft-deleted: booking request was declined (hidden from public)
    Serie      // recurring appointment occurrence, public + blocks external bookings
}
