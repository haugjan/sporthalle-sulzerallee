using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace SporthalleWeb.Infrastructure.Reservierung.Persistence.DbRecords;

[TableName("BookingAuditLog")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class BookingAuditLogRecord
{
    [Column("Id")]
    [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)]
    public long Id { get; set; }

    [Column("EntityType")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    [Length(50)]
    public string EntityType { get; set; } = "";

    [Column("EntityId")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    public int EntityId { get; set; }

    [Column("Action")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    [Length(50)]
    public string Action { get; set; } = "";

    [Column("ChangedBy")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    [Length(200)]
    public string ChangedBy { get; set; } = "";

    [Column("ChangedAt")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    public DateTime ChangedAt { get; set; }

    [Column("OldStatusJson")]
    [NullSetting(NullSetting = NullSettings.Null)]
    [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
    public string? OldStatusJson { get; set; }

    [Column("NewStatusJson")]
    [NullSetting(NullSetting = NullSettings.Null)]
    [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
    public string? NewStatusJson { get; set; }

    [Column("RemoteIp")]
    [NullSetting(NullSetting = NullSettings.Null)]
    [Length(45)]
    public string? RemoteIp { get; set; }

    [Column("Notes")]
    [NullSetting(NullSetting = NullSettings.Null)]
    [Length(500)]
    public string? Notes { get; set; }
}
