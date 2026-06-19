using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Migrations.Upgrade;
using SporthalleWeb.Infrastructure.Reservierung.Persistence.DbRecords;

namespace SporthalleWeb.Infrastructure.Reservierung.Persistence;

public class ReservierungMigrationPlan : MigrationPlan
{
    public ReservierungMigrationPlan() : base("Reservierung")
    {
        From(string.Empty)
            .To<CreateBookingSlotsV1>("v1.0.0");
    }
}

public class CreateBookingSlotsV1 : AsyncMigrationBase
{
    public CreateBookingSlotsV1(IMigrationContext context) : base(context) { }

    protected override Task MigrateAsync()
    {
        if (!TableExists("BookingSlots"))
            Create.Table<BookingSlotRecord>().Do();
        return Task.CompletedTask;
    }
}

public class ReservierungMigrationComponent : IAsyncComponent
{
    private readonly ICoreScopeProvider _scopeProvider;
    private readonly IMigrationPlanExecutor _migrationPlanExecutor;
    private readonly IKeyValueService _keyValueService;
    private readonly IRuntimeState _runtimeState;

    public ReservierungMigrationComponent(
        ICoreScopeProvider scopeProvider,
        IMigrationPlanExecutor migrationPlanExecutor,
        IKeyValueService keyValueService,
        IRuntimeState runtimeState)
    {
        _scopeProvider = scopeProvider;
        _migrationPlanExecutor = migrationPlanExecutor;
        _keyValueService = keyValueService;
        _runtimeState = runtimeState;
    }

    public async Task InitializeAsync(bool isMainDom, CancellationToken cancellationToken)
    {
        if (_runtimeState.Level < RuntimeLevel.Run) return;
        var upgrader = new Upgrader(new ReservierungMigrationPlan());
        await upgrader.ExecuteAsync(_migrationPlanExecutor, _scopeProvider, _keyValueService);
    }

    public Task TerminateAsync(bool isMainDom, CancellationToken cancellationToken) => Task.CompletedTask;
}
