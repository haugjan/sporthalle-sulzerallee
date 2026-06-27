using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;


using SporthalleWeb.Domain.Booking;

namespace SporthalleWeb.Infrastructure.Booking;

[TableName("BookingSlots")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class BookingSlotRecord
{
    [Column("Id")]
    [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)]
    public long Id { get; set; }

    [Column("MemberId")]
    [NullSetting(NullSetting = NullSettings.Null)]
    public long? MemberId { get; set; }

    [Column("Type")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    [Length(20)]
    public string Type { get; set; } = "Reserved";

    [Column("StartUtc")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    public DateTime StartUtc { get; set; }

    [Column("EndUtc")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    public DateTime EndUtc { get; set; }

    [Column("Title")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    [Length(200)]
    public string Title { get; set; } = "";

    [Column("Notes")]
    [NullSetting(NullSetting = NullSettings.Null)]
    [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
    public string? Notes { get; set; }

    [Column("CreatedAt")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    public DateTime CreatedAt { get; set; }

    [Column("UpdatedAt")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    public DateTime UpdatedAt { get; set; }

    [Column("CreatedBy")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    [Length(200)]
    public string CreatedBy { get; set; } = "";

    [Column("RecurringSlotId")]
    [NullSetting(NullSetting = NullSettings.Null)]
    public long? RecurringSlotId { get; set; }

    [Column("IsDeleted")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    public bool IsDeleted { get; set; }

    [Column("ShowTitlePublic")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    public bool ShowTitlePublic { get; set; }
}
