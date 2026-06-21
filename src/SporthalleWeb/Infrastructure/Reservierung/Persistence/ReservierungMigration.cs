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
            .To<CreateBookingSlotsV1>("v1.0.0")
            .To<AddAllReservierungTablesV2>("v1.1.0")
            .To<SimplifyDataModelV3>("v1.2.0")
            .To<AddHallConfigTableV4>("v1.3.0");
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

public class AddAllReservierungTablesV2 : AsyncMigrationBase
{
    public AddAllReservierungTablesV2(IMigrationContext context) : base(context) { }

    protected override Task MigrateAsync()
    {
        if (TableExists("BookingSlots") && ColumnExists("BookingSlots", "RenterId"))
        {
            Delete.Table("BookingSlots").Do();
            Create.Table<BookingSlotRecord>().Do();
        }

        if (!TableExists("MagicLinkTokens"))
            Create.Table<MagicLinkTokenRecord>().Do();

        if (!TableExists("BookingAuditLog"))
            Create.Table<BookingAuditLogRecord>().Do();

        return Task.CompletedTask;
    }
}

// v1.2.0 — simplified slot model: SlotType replaces BookingStatus, Title replaces EventType,
// pricing and recurring fields removed, RecurringRules and SchoolHolidays tables dropped.
// No production data exists, so drop/recreate is safe.
public class SimplifyDataModelV3 : AsyncMigrationBase
{
    public SimplifyDataModelV3(IMigrationContext context) : base(context) { }

    protected override Task MigrateAsync()
    {
        if (TableExists("BookingSlots") && !ColumnExists("BookingSlots", "Type"))
        {
            Delete.Table("BookingSlots").Do();
            Create.Table<BookingSlotRecord>().Do();
        }

        if (TableExists("RecurringRules"))
            Delete.Table("RecurringRules").Do();

        if (TableExists("SchoolHolidays"))
            Delete.Table("SchoolHolidays").Do();

        return Task.CompletedTask;
    }
}

public class AddHallConfigTableV4 : AsyncMigrationBase
{
    public AddHallConfigTableV4(IMigrationContext context) : base(context) { }

    protected override Task MigrateAsync()
    {
        if (!TableExists("HallConfig"))
            Create.Table<HallConfigRecord>().Do();
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
