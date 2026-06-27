using SporthalleWeb.Features.Booking;
using SporthalleWeb.Features.Booking;

namespace SporthalleWeb.Features.Booking;

public sealed class DeleteRecurringSlot(
    IRecurringSlots serieRepo,
    IBookingSlots slotRepo)
{
    public async Task ExecuteAsync(int serieId)
    {
        var serie = await serieRepo.FindByIdAsync(serieId)
            ?? throw new DomainException("Serientermin nicht gefunden.");
        await slotRepo.DeleteByRecurringSlotIdAsync(serie.Id);
        await serieRepo.DeleteAsync(serie.Id);
    }
}
