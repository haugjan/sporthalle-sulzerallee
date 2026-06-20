using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace SporthalleWeb.Infrastructure.Reservierung.Persistence.DbRecords;

[TableName("BookingSlots")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class BookingSlotRecord
{
    [Column("Id")]
    [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)]
    public int Id { get; set; }

    [Column("MemberId")]
    [NullSetting(NullSetting = NullSettings.Null)]
    public int? MemberId { get; set; }

    [Column("RecurringRuleId")]
    [NullSetting(NullSetting = NullSettings.Null)]
    public int? RecurringRuleId { get; set; }

    [Column("Status")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    [Length(20)]
    public string Status { get; set; } = "Provisorisch";

    [Column("StartUtc")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    public DateTime StartUtc { get; set; }

    [Column("EndUtc")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    public DateTime EndUtc { get; set; }

    [Column("PricePerBlock")]
    [NullSetting(NullSetting = NullSettings.Null)]
    public decimal? PricePerBlock { get; set; }

    [Column("TotalBlocks")]
    [NullSetting(NullSetting = NullSettings.Null)]
    public int? TotalBlocks { get; set; }

    [Column("TotalPrice")]
    [NullSetting(NullSetting = NullSettings.Null)]
    public decimal? TotalPrice { get; set; }

    [Column("PriceNote")]
    [NullSetting(NullSetting = NullSettings.Null)]
    [Length(500)]
    public string? PriceNote { get; set; }

    [Column("IsRecurringSlot")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    public bool IsRecurringSlot { get; set; }

    [Column("Color")]
    [NullSetting(NullSetting = NullSettings.Null)]
    [Length(7)]
    public string? Color { get; set; }

    [Column("EventType")]
    [NullSetting(NullSetting = NullSettings.Null)]
    [Length(100)]
    public string? EventType { get; set; }

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
}
