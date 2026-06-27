using SporthalleWeb.Features.PassiveMembership.MemberAdmin;
using SporthalleWeb.Features.PassiveMembership.Registration;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Infrastructure.Manifest;

namespace SporthalleWeb.Infrastructure.PassiveMembership;

public class PassiveMemberComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<IPackageManifestReader, PassiveMemberManifestReader>();

        // Persistence
        builder.AddComponent<PassiveMemberMigrationComponent>();
        builder.Services.AddScoped<IPassiveMembers, UmbracoPassiveMembers>();

        // Email
        builder.Services.Configure<BrevoEmailOptions>(builder.Config.GetSection("Brevo"));
        builder.Services.AddHttpClient<BrevoPassiveMemberEmail>();
        builder.Services.AddScoped<IPassiveMemberEmail, BrevoPassiveMemberEmail>();

        // CAPTCHA
        builder.Services.Configure<TurnstileOptions>(builder.Config.GetSection("Turnstile"));
        builder.Services.AddHttpClient<TurnstilePassiveCaptcha>();
        builder.Services.AddScoped<ICaptcha, TurnstilePassiveCaptcha>();

        // Export adapters
        builder.Services.AddSingleton<IPassiveMemberExport, ClosedXmlPassiveMemberExport>();
        builder.Services.AddSingleton<IPassiveMemberAbaninja, AbaninjaPassiveMemberExport>();

        // Application
        builder.Services.AddScoped<RegisterMember>();
        builder.Services.AddScoped<GetFieldStatuses>();
        builder.Services.AddScoped<PassiveMemberAdmin>();
    }
}
