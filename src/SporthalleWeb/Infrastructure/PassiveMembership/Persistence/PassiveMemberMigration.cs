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
            .To<EnsureMemberStatusColumnsMigration>("passivmitglieder-v4")
            .To<AddAccountingTimestampColumnsMigration>("passivmitglieder-v5")
            .To<AddPhoneAndAddressLine2Migration>("passivmitglieder-v6")
            .To<EnsurePhoneAndAddressLine2Migration>("passivmitglieder-v7")
            .To<EnsurePhoneAndAddressLine2ColumnsMigration>("passivmitglieder-v8")
            .To<DropPassivMitgliederTableMigration>("passivmitglieder-v9");
    }
}

// v1 left as a no-op: v2 creates the definitive table schema.
public class CreatePassiveMemberTableMigration : AsyncMigrationBase
{
    public CreatePassiveMemberTableMigration(IMigrationContext context) : base(context) { }
    protected override Task MigrateAsync() => Task.CompletedTask;
}

// v2: drops the table if it exists (handles fresh DBs and partial prior runs),
// then recreates it with IDENTITY PK and explicit PK name (SQL Server requires named constraints).
public class FixPassiveMemberAutoIncrementMigration : AsyncMigrationBase
{
    public FixPassiveMemberAutoIncrementMigration(IMigrationContext context) : base(context) { }

    protected override Task MigrateAsync()
    {
        if (TableExists("PassivMitglieder"))
        {
            if (IndexExists("IX_PassivMitglieder_FieldNumber"))
                Delete.Index("IX_PassivMitglieder_FieldNumber").OnTable("PassivMitglieder").Do();
            Delete.Table("PassivMitglieder").Do();
        }

        Create.Table("PassivMitglieder")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey("PK_PassivMitglieder").Identity()
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
            .WithColumn("Notes").AsString(4000).Nullable()
            .Do();

        Create.Index("IX_PassivMitglieder_FieldNumber")
            .OnTable("PassivMitglieder")
            .OnColumn("FieldNumber")
            .Unique()
            .Do();

        return Task.CompletedTask;
    }
}

// v3 left as a no-op so existing databases at v2 can advance to v3 cleanly.
public class AddMemberStatusColumnsMigration : AsyncMigrationBase
{
    public AddMemberStatusColumnsMigration(IMigrationContext context) : base(context) { }

    protected override Task MigrateAsync() => Task.CompletedTask;
}

// Each migration version runs exactly once (framework-tracked), so no column-existence checks needed.
// v2 recreates the table without these columns, so they are guaranteed absent when v4 runs.
public class EnsureMemberStatusColumnsMigration : AsyncMigrationBase
{
    public EnsureMemberStatusColumnsMigration(IMigrationContext context) : base(context) { }

    protected override Task MigrateAsync()
    {
        Alter.Table("PassivMitglieder").AddColumn("Status").AsString(20).NotNullable().WithDefaultValue("Confirmed").Do();
        Alter.Table("PassivMitglieder").AddColumn("ConfirmedAt").AsDateTime().Nullable().Do();
        Alter.Table("PassivMitglieder").AddColumn("ConfirmedBy").AsString(200).Nullable().Do();
        Alter.Table("PassivMitglieder").AddColumn("PaidBy").AsString(200).Nullable().Do();
        Alter.Table("PassivMitglieder").AddColumn("ExportedToAccounting").AsBoolean().NotNullable().WithDefaultValue(false).Do();
        return Task.CompletedTask;
    }
}

public class AddAccountingTimestampColumnsMigration : AsyncMigrationBase
{
    public AddAccountingTimestampColumnsMigration(IMigrationContext context) : base(context) { }

    protected override Task MigrateAsync()
    {
        Alter.Table("PassivMitglieder").AddColumn("ExportedToAccountingAt").AsDateTime().Nullable().Do();
        Alter.Table("PassivMitglieder").AddColumn("ExportedToAccountingBy").AsString(200).Nullable().Do();
        return Task.CompletedTask;
    }
}

public class AddPhoneAndAddressLine2Migration : AsyncMigrationBase
{
    public AddPhoneAndAddressLine2Migration(IMigrationContext context) : base(context) { }

    protected override Task MigrateAsync()
    {
        Alter.Table("PassivMitglieder").AddColumn("Phone").AsString(50).Nullable().Do();
        Alter.Table("PassivMitglieder").AddColumn("AddressLine2").AsString(300).Nullable().Do();
        return Task.CompletedTask;
    }
}

public class EnsurePhoneAndAddressLine2Migration : AsyncMigrationBase
{
    public EnsurePhoneAndAddressLine2Migration(IMigrationContext context) : base(context) { }

    protected override Task MigrateAsync() => Task.CompletedTask;
}

public class EnsurePhoneAndAddressLine2ColumnsMigration : AsyncMigrationBase
{
    public EnsurePhoneAndAddressLine2ColumnsMigration(IMigrationContext context) : base(context) { }

    protected override Task MigrateAsync() => Task.CompletedTask;
}

// v9: passive members moved to Umbraco Members; drop the dedicated table.
public class DropPassivMitgliederTableMigration : AsyncMigrationBase
{
    public DropPassivMitgliederTableMigration(IMigrationContext context) : base(context) { }

    protected override Task MigrateAsync()
    {
        if (IndexExists("IX_PassivMitglieder_FieldNumber"))
            Delete.Index("IX_PassivMitglieder_FieldNumber").OnTable("PassivMitglieder").Do();
        if (TableExists("PassivMitglieder"))
            Delete.Table("PassivMitglieder").Do();
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
