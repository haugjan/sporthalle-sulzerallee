using Umbraco.Cms.Core.Composing;
using SporthalleWeb.Application.Booking;
using SporthalleWeb.Domain.Booking.Ports;
using SporthalleWeb.Domain.Shared;
using SporthalleWeb.Infrastructure.Booking.Config;
using SporthalleWeb.Infrastructure.Booking.Email;
using SporthalleWeb.Infrastructure.Booking.Export;
using SporthalleWeb.Infrastructure.Booking.Members;
using SporthalleWeb.Domain.Booking.Ports;
using SporthalleWeb.Infrastructure.Booking.Persistence;
using SporthalleWeb.Infrastructure.Shared;

namespace SporthalleWeb.Infrastructure.Booking.Composition;

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
        builder.Services.AddScoped<IBookingSlotRepository, BookingSlotRepository>();
        builder.Services.AddScoped<IRecurringSlotRepository, RecurringSlotRepository>();
        builder.Services.AddScoped<IMagicLinkTokenRepository, MagicLinkTokenRepository>();
        builder.Services.AddScoped<IBookingAuditRepository, BookingAuditRepository>();

        // Infrastructure adapters
        builder.Services.AddScoped<IMemberManagerPort, UmbracoMemberAdapter>();
        builder.Services.AddScoped<IHallConfigurationPort, UmbracoHallConfigurationAdapter>();
        builder.Services.AddScoped<IBookingEmailPort, BrevoBookingEmailAdapter>();
        builder.Services.AddScoped<IBookingCsvPort, BookingCsvAdapter>();
        builder.Services.AddScoped<ICaptchaPort, TurnstileCaptchaAdapter>();

        // Application queries
        builder.Services.AddScoped<GetWeekSlotsQuery>();
        builder.Services.AddScoped<GetAvailableDaysQuery>();
        builder.Services.AddScoped<GetAvailableTimeSlotsQuery>();

        // Config service
        builder.Services.AddScoped<HallConfigService>();

        // Application use cases
        builder.Services.AddScoped<SendMagicLinkUseCase>();
        builder.Services.AddScoped<ValidateMagicLinkUseCase>();
        builder.Services.AddScoped<RegisterRenterUseCase>();
        builder.Services.AddScoped<LoginWithPasswordUseCase>();
        builder.Services.AddScoped<SetPasswordUseCase>();
        builder.Services.AddScoped<RequestPasswordResetUseCase>();
        builder.Services.AddScoped<ResetPasswordUseCase>();
        builder.Services.AddScoped<CreateBookingUseCase>();
        builder.Services.AddScoped<ConfirmBookingUseCase>();
        builder.Services.AddScoped<RejectBookingUseCase>();
        builder.Services.AddScoped<BookingAdminService>();
        builder.Services.AddScoped<CreateRecurringSlotUseCase>();
        builder.Services.AddScoped<UpdateRecurringSlotUseCase>();
        builder.Services.AddScoped<DeleteRecurringSlotUseCase>();
        builder.Services.AddScoped<GetRecurringSlotsQuery>();
    }
}
