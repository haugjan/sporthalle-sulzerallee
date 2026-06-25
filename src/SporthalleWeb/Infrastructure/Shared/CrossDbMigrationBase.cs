using NPoco;
using Umbraco.Cms.Infrastructure.Migrations;

namespace SporthalleWeb.Infrastructure.Shared;

// Umbraco 17's ColumnExists() generates pragma_table_info SQL (SQLite-only) and throws on SQL Azure.
// This base class wraps it with a fallback to INFORMATION_SCHEMA (SQL Azure) so guards work on both.
public abstract class CrossDbMigrationBase(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected bool SafeColumnExists(string tableName, string columnName)
    {
        try
        {
            return ColumnExists(tableName, columnName);
        }
        catch
        {
            return Context.Database.ExecuteScalar<int>(new Sql(
                "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @0 AND COLUMN_NAME = @1",
                tableName, columnName)) > 0;
        }
    }
}
