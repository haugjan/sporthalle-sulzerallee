using SporthalleWeb.Application.PassivMitgliedschaft;
using SporthalleWeb.Domain.PassivMitgliedschaft.Ports;
using SporthalleWeb.Infrastructure.PassivMitgliedschaft.Captcha;
using SporthalleWeb.Infrastructure.PassivMitgliedschaft.Email;
using SporthalleWeb.Infrastructure.PassivMitgliedschaft.Persistence;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace SporthalleWeb.Infrastructure.PassivMitgliedschaft.Composition;

public class PassivMitgliederComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // Persistence
        builder.AddComponent<PassivMitgliederMigrationComponent>();
        builder.Services.AddScoped<IPassivMitgliederRepository, PassivMitgliederRepository>();

        // Email
        builder.Services.Configure<BrevoEmailOptions>(builder.Config.GetSection("Brevo"));
        builder.Services.AddHttpClient<BrevoEmailAdapter>();
        builder.Services.AddScoped<IEmailPort, BrevoEmailAdapter>();

        // CAPTCHA
        builder.Services.Configure<TurnstileOptions>(builder.Config.GetSection("Turnstile"));
        builder.Services.AddHttpClient<TurnstileCaptchaAdapter>();
        builder.Services.AddScoped<ICaptchaPort, TurnstileCaptchaAdapter>();

        // Application use cases
        builder.Services.AddScoped<RegisterMemberUseCase>();
        builder.Services.AddScoped<GetFieldStatusesQuery>();

        // Phase 3: IExcelPort, IAbaninjaCsvPort, AdminService
    }
}
