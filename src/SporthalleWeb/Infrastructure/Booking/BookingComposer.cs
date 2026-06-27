using Umbraco.Cms.Core.Composing;
using SporthalleWeb.Features.Booking;
using SporthalleWeb.Features.Booking;
using SporthalleWeb.Features.Booking;
using SporthalleWeb.Infrastructure.Booking;
using SporthalleWeb.Infrastructure.Booking;
using SporthalleWeb.Infrastructure.Booking;
using SporthalleWeb.Infrastructure.Shared;
using SporthalleWeb.Features.Booking;
using SporthalleWeb.Infrastructure.Booking;
using SporthalleWeb.Infrastructure.Shared;


using SporthalleWeb.Domain.Booking;

namespace SporthalleWeb.Infrastructure.Booking;

public class BookingComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // Migration
        builder.AddComponent<BookingMigrationComponent>();

        // HTTP clients
        builder.Services.AddHttpClient("Brevo");
        builder.Services.AddHttpClient("Turnstile");

        // Repositories
        builder.Services.AddScoped<IBookingSlots, BookingSlotRepository>();
        builder.Services.AddScoped<IRecurringSlots, RecurringSlotRepository>();
        builder.Services.AddScoped<IMagicLinkTokens, MagicLinkTokenRepository>();
        builder.Services.AddScoped<IBookingAudit, BookingAuditRepository>();

        // Infrastructure adapters
        builder.Services.AddScoped<IHallMembers, UmbracoHallMembers>();
        builder.Services.AddScoped<IHallConfiguration, UmbracoHallConfiguration>();
        builder.Services.AddScoped<IBookingEmail, BrevoBookingEmail>();
        builder.Services.AddScoped<IBookingCsv, BookingCsvExport>();
        builder.Services.AddScoped<ICaptcha, TurnstileBookingCaptcha>();

        // Application queries
        builder.Services.AddScoped<GetWeekSlots>();
        builder.Services.AddScoped<GetAvailableDays>();
        builder.Services.AddScoped<GetAvailableTimeSlots>();

        // Config service
        builder.Services.AddScoped<HallConfigService>();

        // Application use cases
        builder.Services.AddScoped<SendMagicLink>();
        builder.Services.AddScoped<ValidateMagicLink>();
        builder.Services.AddScoped<RegisterRenter>();
        builder.Services.AddScoped<LoginWithPassword>();
        builder.Services.AddScoped<SetPassword>();
        builder.Services.AddScoped<RequestPasswordReset>();
        builder.Services.AddScoped<ResetPassword>();
        builder.Services.AddScoped<CreateBooking>();
        builder.Services.AddScoped<ConfirmBooking>();
        builder.Services.AddScoped<RejectBooking>();
        builder.Services.AddScoped<BookingAdminService>();
        builder.Services.AddScoped<CreateRecurringSlot>();
        builder.Services.AddScoped<UpdateRecurringSlot>();
        builder.Services.AddScoped<DeleteRecurringSlot>();
        builder.Services.AddScoped<GetRecurringSlots>();
    }
}
