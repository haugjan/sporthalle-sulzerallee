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
            .To<EnsurePhoneAndAddressLine2ColumnsMigration>("passivmitglieder-v8");
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

public class EnsureMemberStatusColumnsMigration : AsyncMigrationBase
{
    public EnsureMemberStatusColumnsMigration(IMigrationContext context) : base(context) { }

    protected override Task MigrateAsync()
    {
        if (!ColumnExists("PassivMitglieder", "Status"))
            Alter.Table("PassivMitglieder").AddColumn("Status").AsString(20).NotNullable().WithDefaultValue("Confirmed").Do();

        if (!ColumnExists("PassivMitglieder", "ConfirmedAt"))
            Alter.Table("PassivMitglieder").AddColumn("ConfirmedAt").AsDateTime2().Nullable().Do();

        if (!ColumnExists("PassivMitglieder", "ConfirmedBy"))
            Alter.Table("PassivMitglieder").AddColumn("ConfirmedBy").AsString(200).Nullable().Do();

        if (!ColumnExists("PassivMitglieder", "PaidBy"))
            Alter.Table("PassivMitglieder").AddColumn("PaidBy").AsString(200).Nullable().Do();

        if (!ColumnExists("PassivMitglieder", "ExportedToAccounting"))
            Alter.Table("PassivMitglieder").AddColumn("ExportedToAccounting").AsBoolean().NotNullable().WithDefaultValue(false).Do();

        return Task.CompletedTask;
    }
}

public class AddAccountingTimestampColumnsMigration : AsyncMigrationBase
{
    public AddAccountingTimestampColumnsMigration(IMigrationContext context) : base(context) { }

    protected override Task MigrateAsync()
    {
        if (!ColumnExists("PassivMitglieder", "ExportedToAccountingAt"))
            Alter.Table("PassivMitglieder").AddColumn("ExportedToAccountingAt").AsDateTime2().Nullable().Do();

        if (!ColumnExists("PassivMitglieder", "ExportedToAccountingBy"))
            Alter.Table("PassivMitglieder").AddColumn("ExportedToAccountingBy").AsString(200).Nullable().Do();

        return Task.CompletedTask;
    }
}

public class AddPhoneAndAddressLine2Migration : AsyncMigrationBase
{
    public AddPhoneAndAddressLine2Migration(IMigrationContext context) : base(context) { }

    protected override Task MigrateAsync()
    {
        if (!ColumnExists("PassivMitglieder", "Phone"))
            Alter.Table("PassivMitglieder").AddColumn("Phone").AsString(50).Nullable().Do();

        if (!ColumnExists("PassivMitglieder", "AddressLine2"))
            Alter.Table("PassivMitglieder").AddColumn("AddressLine2").AsString(300).Nullable().Do();

        return Task.CompletedTask;
    }
}

public class EnsurePhoneAndAddressLine2Migration : AsyncMigrationBase
{
    public EnsurePhoneAndAddressLine2Migration(IMigrationContext context) : base(context) { }

    protected override Task MigrateAsync()
    {
        if (!ColumnExists("PassivMitglieder", "Phone"))
            Alter.Table("PassivMitglieder").AddColumn("Phone").AsString(50).Nullable().Do();

        if (!ColumnExists("PassivMitglieder", "AddressLine2"))
            Alter.Table("PassivMitglieder").AddColumn("AddressLine2").AsString(300).Nullable().Do();

        return Task.CompletedTask;
    }
}

public class EnsurePhoneAndAddressLine2ColumnsMigration : AsyncMigrationBase
{
    public EnsurePhoneAndAddressLine2ColumnsMigration(IMigrationContext context) : base(context) { }

    protected override Task MigrateAsync()
    {
        if (!ColumnExists("PassivMitglieder", "Phone"))
            Alter.Table("PassivMitglieder").AddColumn("Phone").AsString(50).Nullable().Do();

        if (!ColumnExists("PassivMitglieder", "AddressLine2"))
            Alter.Table("PassivMitglieder").AddColumn("AddressLine2").AsString(300).Nullable().Do();

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
