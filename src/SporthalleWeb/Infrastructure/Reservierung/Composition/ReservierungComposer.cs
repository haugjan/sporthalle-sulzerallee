using Umbraco.Cms.Core.Composing;
using SporthalleWeb.Application.Reservierung;
using SporthalleWeb.Domain.Reservierung.Ports;
using SporthalleWeb.Infrastructure.Reservierung.Persistence;

namespace SporthalleWeb.Infrastructure.Reservierung.Composition;

public class ReservierungComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AddComponent<ReservierungMigrationComponent>();

        builder.Services.AddScoped<IBookingSlotRepository, BookingSlotRepository>();
        builder.Services.AddScoped<GetWeekSlotsQuery>();
    }
}
