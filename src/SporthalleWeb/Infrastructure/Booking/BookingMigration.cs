using SporthalleWeb.Infrastructure.Booking;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Migrations.Upgrade;


using SporthalleWeb.Domain.Booking;

namespace SporthalleWeb.Infrastructure.Booking;

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
            .To<AddIsBlockerAndMemberIdToRecurringSlotsV7>("v1.6.0")
            .To<AddSoftDeleteToBookingSlotsV8>("v1.7.0")
            .To<AddSoftDeleteToRecurringSlotsV9>("v1.8.0")
            .To<AddShowTitlePublicV10>("v1.9.0")
            .To<DropMagicLinkTokensV11>("v1.10.0")
            .To<DropSlotColorColumnsV12>("v1.11.0")
            .To<AlignStateV13>("v1.12.0")
        .To<MakeAllMemberPropertiesOptionalV14>("v1.13.0");
    }
}

public class CreateBookingSlotsV1(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        if (!TableExists("BookingSlots"))
            Create.Table<BookingSlotRecord>().Do();
        return Task.CompletedTask;
    }
}

public class AddAllBookingTablesV2(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        if (!TableExists("BookingAuditLog"))
            Create.Table<BookingAuditLogRecord>().Do();

        return Task.CompletedTask;
    }
}

public class SimplifyDataModelV3(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        if (TableExists("RecurringRules"))
            Delete.Table("RecurringRules").Do();

        if (TableExists("SchoolHolidays"))
            Delete.Table("SchoolHolidays").Do();

        return Task.CompletedTask;
    }
}

public class AddHallConfigTableV4(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        if (!TableExists("HallConfig"))
            Create.Table<HallConfigRecord>().Do();
        return Task.CompletedTask;
    }
}

public class AddRecurringSlotsV5(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        if (!TableExists("RecurringSlots"))
            Create.Table<RecurringSlotRecord>().Do();

        if (TableExists("BookingSlots"))
            Execute.Sql("ALTER TABLE \"BookingSlots\" ADD \"RecurringSlotId\" INTEGER NULL").Do();

        return Task.CompletedTask;
    }
}

public class RenameSerieToRecurringV6(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        Execute.Sql("UPDATE \"BookingSlots\" SET \"Type\" = 'Recurring' WHERE \"Type\" = 'Serie'").Do();
        return Task.CompletedTask;
    }
}

public class AddIsBlockerAndMemberIdToRecurringSlotsV7(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        if (TableExists("RecurringSlots"))
        {
            Execute.Sql("ALTER TABLE \"RecurringSlots\" ADD \"IsBlocker\" INTEGER NOT NULL DEFAULT 0").Do();
            Execute.Sql(
                "UPDATE \"RecurringSlots\" SET \"IsBlocker\" = 1 WHERE \"Id\" IN " +
                "(SELECT DISTINCT \"RecurringSlotId\" FROM \"BookingSlots\" " +
                " WHERE \"Type\" = 'Blocker' AND \"RecurringSlotId\" IS NOT NULL)").Do();
            Execute.Sql("ALTER TABLE \"RecurringSlots\" ADD \"MemberId\" INTEGER NULL").Do();
        }
        return Task.CompletedTask;
    }
}

public class AddSoftDeleteToBookingSlotsV8(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        Execute.Sql("ALTER TABLE \"BookingSlots\" ADD \"IsDeleted\" INTEGER NOT NULL DEFAULT 0").Do();
        return Task.CompletedTask;
    }
}

public class AddSoftDeleteToRecurringSlotsV9(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        Execute.Sql("ALTER TABLE \"RecurringSlots\" ADD \"IsDeleted\" INTEGER NOT NULL DEFAULT 0").Do();
        return Task.CompletedTask;
    }
}

public class AddShowTitlePublicV10(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        Execute.Sql("ALTER TABLE \"BookingSlots\" ADD \"ShowTitlePublic\" INTEGER NOT NULL DEFAULT 0").Do();
        Execute.Sql("ALTER TABLE \"RecurringSlots\" ADD \"ShowTitlePublic\" INTEGER NOT NULL DEFAULT 0").Do();
        return Task.CompletedTask;
    }
}

public class DropMagicLinkTokensV11(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        if (TableExists("MagicLinkTokens"))
            Delete.Table("MagicLinkTokens").Do();
        return Task.CompletedTask;
    }
}

public class DropSlotColorColumnsV12(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        if (ColumnExists("BookingSlots", "Color"))
            Delete.Column("Color").FromTable("BookingSlots").Do();
        if (ColumnExists("RecurringSlots", "Color"))
            Delete.Column("Color").FromTable("RecurringSlots").Do();
        return Task.CompletedTask;
    }
}

public class AlignStateV13(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync() => Task.CompletedTask;
}

public class MakeAllMemberPropertiesOptionalV14(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        Execute.Sql(
            "UPDATE cmsPropertyType SET mandatory = 0 " +
            "WHERE contentTypeId IN (" +
            "  SELECT nodeId FROM cmsContentType WHERE alias IN ('hallMember', 'passivMember')" +
            ")").Do();
        return Task.CompletedTask;
    }
}

public class BookingMigrationComponent(
    ICoreScopeProvider scopeProvider,
    IMigrationPlanExecutor migrationPlanExecutor,
    IKeyValueService keyValueService,
    IRuntimeState runtimeState)
    : IAsyncComponent
{
    public async Task InitializeAsync(bool isMainDom, CancellationToken cancellationToken)
    {
        if (runtimeState.Level < RuntimeLevel.Run) return;
        var upgrader = new Upgrader(new BookingMigrationPlan());
        await upgrader.ExecuteAsync(migrationPlanExecutor, scopeProvider, keyValueService);
    }

    public Task TerminateAsync(bool isMainDom, CancellationToken cancellationToken) => Task.CompletedTask;
}
