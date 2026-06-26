using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace SporthalleWeb.Infrastructure.Booking.Persistence.DbRecords;

[TableName("RecurringSlots")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class RecurringSlotRecord
{
    [Column("Id")]
    [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)]
    public long Id { get; set; }

    [Column("Title")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    [Length(200)]
    public string Title { get; set; } = "";

    [Column("Wochentag")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    public int Wochentag { get; set; }

    [Column("StartTime")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    [Length(5)]
    public string StartTime { get; set; } = "";

    [Column("EndTime")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    [Length(5)]
    public string EndTime { get; set; } = "";

    [Column("SeriesStart")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    [Length(10)]
    public string SeriesStart { get; set; } = "";

    [Column("SeriesEnd")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    [Length(10)]
    public string SeriesEnd { get; set; } = "";

    [Column("Color")]
    [NullSetting(NullSetting = NullSettings.Null)]
    [Length(7)]
    public string? Color { get; set; }

    [Column("Notes")]
    [NullSetting(NullSetting = NullSettings.Null)]
    [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
    public string? Notes { get; set; }

    [Column("IsBlocker")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    public bool IsBlocker { get; set; }

    [Column("MemberId")]
    [NullSetting(NullSetting = NullSettings.Null)]
    public int? MemberId { get; set; }

    [Column("CreatedBy")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    [Length(200)]
    public string CreatedBy { get; set; } = "";

    [Column("CreatedAt")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    public DateTime CreatedAt { get; set; }

    [Column("UpdatedAt")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    public DateTime UpdatedAt { get; set; }

    [Column("IsDeleted")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    public bool IsDeleted { get; set; }

    [Column("ShowTitlePublic")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    public bool ShowTitlePublic { get; set; }
}
