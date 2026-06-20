using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace SporthalleWeb.Infrastructure.Reservierung.Persistence.DbRecords;

[TableName("RecurringRules")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class RecurringRuleRecord
{
    [Column("Id")]
    [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)]
    public int Id { get; set; }

    [Column("MemberId")]
    [NullSetting(NullSetting = NullSettings.Null)]
    public int? MemberId { get; set; }

    [Column("Description")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    [Length(300)]
    public string Description { get; set; } = "";

    [Column("DayOfWeek")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    public int DayOfWeek { get; set; }

    [Column("StartTime")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    [Length(5)]
    public string StartTime { get; set; } = "";

    [Column("EndTime")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    [Length(5)]
    public string EndTime { get; set; } = "";

    [Column("ValidFrom")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    public DateTime ValidFrom { get; set; }

    [Column("ValidUntil")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    public DateTime ValidUntil { get; set; }

    [Column("IntervalWeeks")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    public int IntervalWeeks { get; set; } = 1;

    [Column("IsActive")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    public bool IsActive { get; set; } = true;

    [Column("ExcludeSchoolHolidays")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    public bool ExcludeSchoolHolidays { get; set; }

    [Column("Color")]
    [NullSetting(NullSetting = NullSettings.Null)]
    [Length(7)]
    public string? Color { get; set; }

    [Column("Notes")]
    [NullSetting(NullSetting = NullSettings.Null)]
    [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
    public string? Notes { get; set; }

    [Column("CreatedAt")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    public DateTime CreatedAt { get; set; }

    [Column("CreatedBy")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    [Length(200)]
    public string CreatedBy { get; set; } = "";
}
