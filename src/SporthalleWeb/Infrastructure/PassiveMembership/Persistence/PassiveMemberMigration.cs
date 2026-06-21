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
            .To<FixPassiveMemberAutoIncrementMigration>("passivmitglieder-v2");
    }
}

public class CreatePassiveMemberTableMigration : MigrationBase
{
    public CreatePassiveMemberTableMigration(IMigrationContext context) : base(context) { }

    protected override void Migrate()
    {
        if (!TableExists("PassivMitglieder"))
        {
            Create.Table<PassiveMemberDbRecord>().Do();

            // Unique constraint on FieldNumber (not handled by [PrimaryKey])
            Create.Index("IX_PassivMitglieder_FieldNumber")
                .OnTable("PassivMitglieder")
                .OnColumn("FieldNumber")
                .Unique()
                .Do();
        }
    }
}

public class FixPassiveMemberAutoIncrementMigration : MigrationBase
{
    public FixPassiveMemberAutoIncrementMigration(IMigrationContext context) : base(context) { }

    protected override void Migrate()
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
            .WithColumn("Notes").AsCustom("nvarchar(max)").Nullable()
            .Do();

        Create.Index("IX_PassivMitglieder_FieldNumber")
            .OnTable("PassivMitglieder")
            .OnColumn("FieldNumber")
            .Unique()
            .Do();
    }
}

public class PassiveMemberMigrationComponent : IComponent
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

    public void Initialize()
    {
        if (_runtimeState.Level < RuntimeLevel.Run) return;

        var plan = new PassiveMemberMigrationPlan();
        var upgrader = new Upgrader(plan);
        upgrader.Execute(_migrationPlanExecutor, _scopeProvider, _keyValueService);
    }

    public void Terminate() { }
}
