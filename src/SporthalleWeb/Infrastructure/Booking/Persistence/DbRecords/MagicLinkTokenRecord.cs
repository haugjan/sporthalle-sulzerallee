using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace SporthalleWeb.Infrastructure.Booking.Persistence.DbRecords;

[TableName("MagicLinkTokens")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class MagicLinkTokenRecord
{
    [Column("Id")]
    [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)]
    public int Id { get; set; }

    [Column("MemberId")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    public int MemberId { get; set; }

    [Column("TokenHash")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    [Length(128)]
    public string TokenHash { get; set; } = "";

    [Column("ExpiresAt")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    public DateTime ExpiresAt { get; set; }

    [Column("UsedAt")]
    [NullSetting(NullSetting = NullSettings.Null)]
    public DateTime? UsedAt { get; set; }

    [Column("CreatedAt")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    public DateTime CreatedAt { get; set; }

    [Column("RemoteIp")]
    [NullSetting(NullSetting = NullSettings.Null)]
    [Length(45)]
    public string? RemoteIp { get; set; }
}
