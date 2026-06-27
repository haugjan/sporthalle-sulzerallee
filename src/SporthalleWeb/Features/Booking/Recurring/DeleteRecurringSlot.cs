using SporthalleWeb.Domain.Booking.SlotAggregate;
using SporthalleWeb.Features.Booking.Ports;

namespace SporthalleWeb.Features.Booking.Recurring;

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
