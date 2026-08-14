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
        builder.AddComponent<BookingMigrationComponent>();

        builder.Services.AddHttpClient("Turnstile");

        builder.Services.AddScoped<IBookingSlots, BookingSlotRepository>();
        builder.Services.AddScoped<IRecurringSlots, RecurringSlotRepository>();
        builder.Services.AddScoped<IBookingAudit, BookingAuditRepository>();

        builder.Services.AddScoped<IHallMembers, UmbracoHallMembers>();
        builder.Services.AddScoped<IHallConfiguration, UmbracoHallConfiguration>();
        builder.Services.AddScoped<IBookingEmail, BookingEmailSender>();
        builder.Services.AddScoped<IBookingCsv, BookingCsvExport>();
        builder.Services.AddScoped<ICaptcha, TurnstileBookingCaptcha>();

        builder.Services.AddScoped<GetWeekSlots>();
        builder.Services.AddScoped<GetAvailableDays>();
        builder.Services.AddScoped<GetAvailableTimeSlots>();

        builder.Services.AddScoped<IHallConfigStore, UmbracoHallConfigStore>();

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
