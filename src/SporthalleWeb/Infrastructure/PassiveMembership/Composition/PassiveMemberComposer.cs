using SporthalleWeb.Application.PassiveMembership;
using SporthalleWeb.Domain.PassiveMembership.Ports;
using SporthalleWeb.Infrastructure.PassiveMembership.Captcha;
using SporthalleWeb.Infrastructure.PassiveMembership.Email;
using SporthalleWeb.Infrastructure.PassiveMembership.Excel;
using SporthalleWeb.Infrastructure.PassiveMembership.Persistence;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Infrastructure.Manifest;

namespace SporthalleWeb.Infrastructure.PassiveMembership.Composition;

public class PassiveMemberComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<IPackageManifestReader, PassiveMemberManifestReader>();

        // Persistence
        builder.AddComponent<PassiveMemberMigrationComponent>();
        builder.Services.AddScoped<IPassiveMemberRepository, PassiveMemberRepository>();

        // Email
        builder.Services.Configure<BrevoEmailOptions>(builder.Config.GetSection("Brevo"));
        builder.Services.AddHttpClient<BrevoEmailAdapter>();
        builder.Services.AddScoped<IEmailPort, BrevoEmailAdapter>();

        // CAPTCHA
        builder.Services.Configure<TurnstileOptions>(builder.Config.GetSection("Turnstile"));
        builder.Services.AddHttpClient<TurnstileCaptchaAdapter>();
        builder.Services.AddScoped<ICaptchaPort, TurnstileCaptchaAdapter>();

        // Export adapters
        builder.Services.AddSingleton<IExcelPort, ClosedXmlExcelAdapter>();
        builder.Services.AddSingleton<IAbaninjaCsvPort, AbaninjaCsvAdapter>();

        // Application use cases
        builder.Services.AddScoped<RegisterMemberUseCase>();
        builder.Services.AddScoped<GetFieldStatusesQuery>();
        builder.Services.AddScoped<AdminService>();
    }
}
