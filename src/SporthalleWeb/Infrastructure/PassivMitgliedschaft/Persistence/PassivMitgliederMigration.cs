using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Migrations.Upgrade;

namespace SporthalleWeb.Infrastructure.PassivMitgliedschaft.Persistence;

public class PassivMitgliederMigrationPlan : MigrationPlan
{
    public PassivMitgliederMigrationPlan() : base("PassivMitglieder")
    {
        From(string.Empty).To<CreatePassivMitgliederTableMigration>("passivmitglieder-v1");
    }
}

public class CreatePassivMitgliederTableMigration : MigrationBase
{
    public CreatePassivMitgliederTableMigration(IMigrationContext context) : base(context) { }

    protected override void Migrate()
    {
        if (!TableExists("PassivMitglieder"))
        {
            Create.Table<PassivMitgliedDbRecord>().Do();

            // Unique constraint on FieldNumber (not handled by [PrimaryKey])
            Create.Index("IX_PassivMitglieder_FieldNumber")
                .OnTable("PassivMitglieder")
                .OnColumn("FieldNumber")
                .Unique()
                .Do();
        }
    }
}

public class PassivMitgliederMigrationComponent : IComponent
{
    private readonly ICoreScopeProvider _scopeProvider;
    private readonly IMigrationPlanExecutor _migrationPlanExecutor;
    private readonly IKeyValueService _keyValueService;
    private readonly IRuntimeState _runtimeState;

    public PassivMitgliederMigrationComponent(
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

        var plan = new PassivMitgliederMigrationPlan();
        var upgrader = new Upgrader(plan);
        upgrader.Execute(_migrationPlanExecutor, _scopeProvider, _keyValueService);
    }

    public void Terminate() { }
}
