using Umbraco.Cms.Core.Composing;
using SporthalleWeb.Application.Reservierung;
using SporthalleWeb.Domain.Reservierung.Ports;
using SporthalleWeb.Infrastructure.Reservierung.Members;
using SporthalleWeb.Infrastructure.Reservierung.Persistence;

namespace SporthalleWeb.Infrastructure.Reservierung.Composition;

public class ReservierungComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // Migration
        builder.AddComponent<ReservierungMigrationComponent>();

        // Repositories
        builder.Services.AddScoped<IBookingSlotRepository, BookingSlotRepository>();
        builder.Services.AddScoped<IRecurringRuleRepository, RecurringRuleRepository>();
        builder.Services.AddScoped<IMagicLinkTokenRepository, MagicLinkTokenRepository>();
        builder.Services.AddScoped<IBookingAuditRepository, BookingAuditRepository>();
        builder.Services.AddScoped<SchoolHolidayRepository>();

        // Member adapter
        builder.Services.AddScoped<IMemberManagerPort, UmbracoMemberAdapter>();

        // Application queries
        builder.Services.AddScoped<GetWeekSlotsQuery>();
    }
}
