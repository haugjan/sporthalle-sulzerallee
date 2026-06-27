using Umbraco.Cms.Core.Composing;
using SporthalleWeb.Features.Booking.Admin;
using SporthalleWeb.Features.Booking.Calendar;
using SporthalleWeb.Features.Booking.Configuration;
using SporthalleWeb.Features.Booking.Ports;
using SporthalleWeb.Features.Booking.Recurring;
using SporthalleWeb.Features.Booking.Requests;
using SporthalleWeb.Infrastructure.Booking;
using SporthalleWeb.Infrastructure.Shared;

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

        // Config store (raw key-value)
        builder.Services.AddScoped<IHallConfigStore, UmbracoHallConfigStore>();

        // Application use cases
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
