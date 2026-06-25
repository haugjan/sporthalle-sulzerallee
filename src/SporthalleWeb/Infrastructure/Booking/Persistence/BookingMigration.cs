using SporthalleWeb.Infrastructure.Booking.Persistence.DbRecords;
using SporthalleWeb.Infrastructure.Shared;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Migrations.Upgrade;

namespace SporthalleWeb.Infrastructure.Booking.Persistence;

public class BookingMigrationPlan : MigrationPlan
{
    public BookingMigrationPlan() : base("Reservierung")
    {
        From(string.Empty)
            .To<CreateBookingSlotsV1>("v1.0.0")
            .To<AddAllBookingTablesV2>("v1.1.0")
            .To<SimplifyDataModelV3>("v1.2.0")
            .To<AddHallConfigTableV4>("v1.3.0")
            .To<AddRecurringSlotsV5>("v1.4.0")
            .To<RenameSerieToRecurringV6>("v1.5.0")
            .To<AddIsBlockerAndMemberIdToRecurringSlotsV7>("v1.6.0");
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

public class AddAllBookingTablesV2 : AsyncMigrationBase
{
    public AddAllBookingTablesV2(IMigrationContext context) : base(context) { }

    protected override Task MigrateAsync()
    {
        // Drop/recreate for old RenterId schema was only needed for pre-v1 databases;
        // v1 always creates BookingSlots with the current schema, so this block is omitted.

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
        // Drop/recreate for pre-Type schema omitted: v1 always creates BookingSlots with Type column.

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

public class AddRecurringSlotsV5 : CrossDbMigrationBase
{
    public AddRecurringSlotsV5(IMigrationContext context) : base(context) { }

    protected override Task MigrateAsync()
    {
        if (!TableExists("RecurringSlots"))
            Create.Table<RecurringSlotRecord>().Do();

        if (TableExists("BookingSlots") && !SafeColumnExists("BookingSlots", "RecurringSlotId"))
            Execute.Sql("ALTER TABLE \"BookingSlots\" ADD \"RecurringSlotId\" INTEGER NULL").Do();

        return Task.CompletedTask;
    }
}

// v1.6.0 — Add IsBlocker and MemberId columns to RecurringSlots; backfill IsBlocker from BookingSlots.
public class AddIsBlockerAndMemberIdToRecurringSlotsV7 : CrossDbMigrationBase
{
    public AddIsBlockerAndMemberIdToRecurringSlotsV7(IMigrationContext context) : base(context) { }

    protected override Task MigrateAsync()
    {
        if (TableExists("RecurringSlots") && !SafeColumnExists("RecurringSlots", "IsBlocker"))
        {
            Execute.Sql("ALTER TABLE \"RecurringSlots\" ADD \"IsBlocker\" INTEGER NOT NULL DEFAULT 0").Do();
            Execute.Sql(
                "UPDATE \"RecurringSlots\" SET \"IsBlocker\" = 1 WHERE \"Id\" IN " +
                "(SELECT DISTINCT \"RecurringSlotId\" FROM \"BookingSlots\" " +
                " WHERE \"Type\" = 'Blocker' AND \"RecurringSlotId\" IS NOT NULL)").Do();
        }
        if (TableExists("RecurringSlots") && !SafeColumnExists("RecurringSlots", "MemberId"))
            Execute.Sql("ALTER TABLE \"RecurringSlots\" ADD \"MemberId\" INTEGER NULL").Do();
        return Task.CompletedTask;
    }
}

// v1.5.0 — SlotType.Serie was renamed to SlotType.Recurring; update existing DB rows.
public class RenameSerieToRecurringV6 : AsyncMigrationBase
{
    public RenameSerieToRecurringV6(IMigrationContext context) : base(context) { }

    protected override Task MigrateAsync()
    {
        Execute.Sql("UPDATE \"BookingSlots\" SET \"Type\" = 'Recurring' WHERE \"Type\" = 'Serie'").Do();
        return Task.CompletedTask;
    }
}

public class BookingMigrationComponent : IAsyncComponent
{
    private readonly ICoreScopeProvider _scopeProvider;
    private readonly IMigrationPlanExecutor _migrationPlanExecutor;
    private readonly IKeyValueService _keyValueService;
    private readonly IRuntimeState _runtimeState;

    public BookingMigrationComponent(
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
        var upgrader = new Upgrader(new BookingMigrationPlan());
        await upgrader.ExecuteAsync(_migrationPlanExecutor, _scopeProvider, _keyValueService);
    }

    public Task TerminateAsync(bool isMainDom, CancellationToken cancellationToken) => Task.CompletedTask;
}
