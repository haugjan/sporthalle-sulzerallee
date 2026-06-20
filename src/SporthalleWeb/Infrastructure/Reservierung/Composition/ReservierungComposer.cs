using Umbraco.Cms.Core.Composing;
using SporthalleWeb.Application.Reservierung;
using SporthalleWeb.Domain.Reservierung.Ports;
using SporthalleWeb.Domain.Shared;
using SporthalleWeb.Infrastructure.Reservierung.Config;
using SporthalleWeb.Infrastructure.Reservierung.Email;
using SporthalleWeb.Infrastructure.Reservierung.Export;
using SporthalleWeb.Infrastructure.Reservierung.Members;
using SporthalleWeb.Infrastructure.Reservierung.Persistence;
using SporthalleWeb.Infrastructure.Shared;

namespace SporthalleWeb.Infrastructure.Reservierung.Composition;

public class ReservierungComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // Migration
        builder.AddComponent<ReservierungMigrationComponent>();

        // HTTP clients
        builder.Services.AddHttpClient("Brevo");
        builder.Services.AddHttpClient("Turnstile");

        // Repositories
        builder.Services.AddScoped<IBookingSlotRepository, BookingSlotRepository>();
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
    }
}
