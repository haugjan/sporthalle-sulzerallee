using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Migrations.Upgrade;

namespace SporthalleWeb.Infrastructure.Shared;

public class EmailOutboxMigrationPlan : MigrationPlan
{
    public EmailOutboxMigrationPlan() : base("EmailOutbox")
    {
        From(string.Empty).To<CreateOutboxEmailsTable>("email-outbox-v1");
    }
}

public class CreateOutboxEmailsTable(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        if (!TableExists("OutboxEmails"))
            Create.Table<OutboxEmailRecord>().Do();
        return Task.CompletedTask;
    }
}

public class EmailOutboxMigrationComponent(
    ICoreScopeProvider scopeProvider,
    IMigrationPlanExecutor migrationPlanExecutor,
    IKeyValueService keyValueService,
    IRuntimeState runtimeState) : IAsyncComponent
{
    public async Task InitializeAsync(bool isMainDom, CancellationToken cancellationToken)
    {
        if (runtimeState.Level < RuntimeLevel.Run) return;
        var upgrader = new Upgrader(new EmailOutboxMigrationPlan());
        await upgrader.ExecuteAsync(migrationPlanExecutor, scopeProvider, keyValueService);
    }

    public Task TerminateAsync(bool isMainDom, CancellationToken cancellationToken) => Task.CompletedTask;
}
