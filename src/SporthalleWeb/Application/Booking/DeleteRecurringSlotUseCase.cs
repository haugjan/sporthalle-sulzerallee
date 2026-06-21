using SporthalleWeb.Domain.Booking;
using SporthalleWeb.Domain.Booking.Ports;

namespace SporthalleWeb.Application.Booking;

public sealed class DeleteRecurringSlotUseCase(
    IRecurringSlotRepository serieRepo,
    IBookingSlotRepository slotRepo)
{
    public async Task ExecuteAsync(int serieId)
    {
        var serie = await serieRepo.FindByIdAsync(serieId)
            ?? throw new DomainException("Serientermin nicht gefunden.");
        await slotRepo.DeleteByRecurringSlotIdAsync(serie.Id);
        await serieRepo.DeleteAsync(serie.Id);
    }
}
