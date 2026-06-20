namespace SporthalleWeb.Domain.Reservierung;

public enum SlotType
{
    Blocker,   // admin-only placeholder, no member required, not shown publicly
    Reserved,  // unconfirmed booking request, member required
    Booked     // confirmed booking, member required
}
