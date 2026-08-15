using SporthalleWeb.Features.Email;
using Umbraco.Cms.Core.Composing;

namespace SporthalleWeb.Infrastructure.Shared;

public sealed class EmailOutboxComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AddComponent<EmailOutboxMigrationComponent>();

        builder.Services.AddSingleton<GraphMailClient>();
        builder.Services.AddSingleton<OutboxSignal>();

        builder.Services.AddScoped<OutboxRepository>();
        builder.Services.AddScoped<IEmailOutbox>(sp => sp.GetRequiredService<OutboxRepository>());
        builder.Services.AddScoped<IOutboxAdminReport>(sp => sp.GetRequiredService<OutboxRepository>());

        builder.Services.AddHostedService<OutboxDispatcher>();
    }
}
