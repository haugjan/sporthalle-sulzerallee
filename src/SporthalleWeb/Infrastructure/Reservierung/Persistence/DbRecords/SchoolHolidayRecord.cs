using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace SporthalleWeb.Infrastructure.Reservierung.Persistence.DbRecords;

[TableName("SchoolHolidays")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class SchoolHolidayRecord
{
    [Column("Id")]
    [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)]
    public int Id { get; set; }

    [Column("Name")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    [Length(200)]
    public string Name { get; set; } = "";

    [Column("HolidayFrom")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    public DateTime HolidayFrom { get; set; }

    [Column("HolidayUntil")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    public DateTime HolidayUntil { get; set; }

    [Column("CreatedAt")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    public DateTime CreatedAt { get; set; }
}
