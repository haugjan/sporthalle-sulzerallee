using Microsoft.Extensions.Logging;
using NPoco;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Infrastructure.Scoping;

namespace SporthalleWeb.Infrastructure.PassiveMembership.Persistence;

public sealed class PassivMemberSchemaEnsurer : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private readonly IScopeProvider _scopeProvider;
    private readonly ILogger<PassivMemberSchemaEnsurer> _logger;

    public PassivMemberSchemaEnsurer(IScopeProvider scopeProvider, ILogger<PassivMemberSchemaEnsurer> logger)
    {
        _scopeProvider = scopeProvider;
        _logger = logger;
    }

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        var columnsToAdd = new (string Name, string Ddl)[]
        {
            ("Status",               "\"Status\" TEXT NOT NULL DEFAULT 'Confirmed'"),
            ("ConfirmedAt",          "\"ConfirmedAt\" TEXT NULL"),
            ("ConfirmedBy",          "\"ConfirmedBy\" TEXT NULL"),
            ("PaidBy",               "\"PaidBy\" TEXT NULL"),
            ("ExportedToAccounting", "\"ExportedToAccounting\" INTEGER NOT NULL DEFAULT 0"),
        };

        using var scope = _scopeProvider.CreateScope();

        foreach (var (name, ddl) in columnsToAdd)
        {
            var exists = await scope.Database.ExecuteScalarAsync<int>(
                new Sql($"SELECT COUNT(*) FROM pragma_table_info('PassivMitglieder') WHERE name = '{name}'"));

            if (exists == 0)
            {
                await scope.Database.ExecuteAsync(
                    new Sql($"ALTER TABLE \"PassivMitglieder\" ADD COLUMN {ddl}"));
                _logger.LogInformation("PassivMember schema: added column {Column}", name);
            }
        }

        scope.Complete();
    }
}
