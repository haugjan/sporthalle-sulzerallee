using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Migrations.Upgrade;

namespace SporthalleWeb.Infrastructure.PassiveMembership.Persistence;

public class PassiveMemberMigrationPlan : MigrationPlan
{
    public PassiveMemberMigrationPlan() : base("PassivMitglieder")
    {
        From(string.Empty)
            .To<CreatePassiveMemberTableMigration>("passivmitglieder-v1")
            .To<FixPassiveMemberAutoIncrementMigration>("passivmitglieder-v2")
            .To<AddMemberStatusColumnsMigration>("passivmitglieder-v3")
            .To<EnsureMemberStatusColumnsMigration>("passivmitglieder-v4");
    }
}

public class CreatePassiveMemberTableMigration : AsyncMigrationBase
{
    public CreatePassiveMemberTableMigration(IMigrationContext context) : base(context) { }

    protected override Task MigrateAsync()
    {
        if (!TableExists("PassivMitglieder"))
        {
            Create.Table<PassiveMemberDbRecord>().Do();
            Create.Index("IX_PassivMitglieder_FieldNumber")
                .OnTable("PassivMitglieder")
                .OnColumn("FieldNumber")
                .Unique()
                .Do();
        }
        return Task.CompletedTask;
    }
}

public class FixPassiveMemberAutoIncrementMigration : AsyncMigrationBase
{
    public FixPassiveMemberAutoIncrementMigration(IMigrationContext context) : base(context) { }

    protected override Task MigrateAsync()
    {
        if (IndexExists("IX_PassivMitglieder_FieldNumber"))
            Delete.Index("IX_PassivMitglieder_FieldNumber").OnTable("PassivMitglieder").Do();

        Delete.Table("PassivMitglieder").Do();

        Create.Table("PassivMitglieder")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("FieldNumber").AsInt32().NotNullable()
            .WithColumn("FirstName").AsString(100).NotNullable()
            .WithColumn("LastName").AsString(100).NotNullable()
            .WithColumn("AddressLine").AsString(300).NotNullable()
            .WithColumn("PostalCode").AsString(20).NotNullable()
            .WithColumn("City").AsString(100).NotNullable()
            .WithColumn("Country").AsString(100).NotNullable()
            .WithColumn("Email").AsString(200).NotNullable()
            .WithColumn("MembershipLevel").AsString(20).NotNullable()
            .WithColumn("ShowNameOnFloor").AsBoolean().NotNullable()
            .WithColumn("DisplayName").AsString(200).Nullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable()
            .WithColumn("PaidAt").AsDateTime().Nullable()
            .WithColumn("Notes").AsString(int.MaxValue).Nullable()
            .Do();

        Create.Index("IX_PassivMitglieder_FieldNumber")
            .OnTable("PassivMitglieder")
            .OnColumn("FieldNumber")
            .Unique()
            .Do();

        return Task.CompletedTask;
    }
}

// v3 left as a no-op so existing databases at v2 can advance to v3 cleanly;
// v4 does the actual column additions via raw SQL.
public class AddMemberStatusColumnsMigration : AsyncMigrationBase
{
    public AddMemberStatusColumnsMigration(IMigrationContext context) : base(context) { }

    protected override Task MigrateAsync() => Task.CompletedTask;
}

// v4 adds Status and related columns using raw SQLite DDL to avoid Fluent Migrator
// provider translation issues with boolean defaults.
public class EnsureMemberStatusColumnsMigration : AsyncMigrationBase
{
    public EnsureMemberStatusColumnsMigration(IMigrationContext context) : base(context) { }

    protected override Task MigrateAsync()
    {
        if (!ColumnExists("PassivMitglieder", "Status"))
            Execute.Sql("ALTER TABLE \"PassivMitglieder\" ADD COLUMN \"Status\" TEXT NOT NULL DEFAULT 'Confirmed'");

        if (!ColumnExists("PassivMitglieder", "ConfirmedAt"))
            Execute.Sql("ALTER TABLE \"PassivMitglieder\" ADD COLUMN \"ConfirmedAt\" TEXT NULL");

        if (!ColumnExists("PassivMitglieder", "ConfirmedBy"))
            Execute.Sql("ALTER TABLE \"PassivMitglieder\" ADD COLUMN \"ConfirmedBy\" TEXT NULL");

        if (!ColumnExists("PassivMitglieder", "PaidBy"))
            Execute.Sql("ALTER TABLE \"PassivMitglieder\" ADD COLUMN \"PaidBy\" TEXT NULL");

        if (!ColumnExists("PassivMitglieder", "ExportedToAccounting"))
            Execute.Sql("ALTER TABLE \"PassivMitglieder\" ADD COLUMN \"ExportedToAccounting\" INTEGER NOT NULL DEFAULT 0");

        return Task.CompletedTask;
    }
}

public class PassiveMemberMigrationComponent : IAsyncComponent
{
    private readonly ICoreScopeProvider _scopeProvider;
    private readonly IMigrationPlanExecutor _migrationPlanExecutor;
    private readonly IKeyValueService _keyValueService;
    private readonly IRuntimeState _runtimeState;

    public PassiveMemberMigrationComponent(
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
        var upgrader = new Upgrader(new PassiveMemberMigrationPlan());
        await upgrader.ExecuteAsync(_migrationPlanExecutor, _scopeProvider, _keyValueService);
    }

    public Task TerminateAsync(bool isMainDom, CancellationToken cancellationToken) => Task.CompletedTask;
}
