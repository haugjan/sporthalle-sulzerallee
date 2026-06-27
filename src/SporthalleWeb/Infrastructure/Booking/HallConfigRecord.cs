using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace SporthalleWeb.Infrastructure.Booking;

[TableName("HallConfig")]
[PrimaryKey("Key", AutoIncrement = false)]
[ExplicitColumns]
public class HallConfigRecord
{
    [Column("Key")]
    [PrimaryKeyColumn(AutoIncrement = false)]
    [Length(100)]
    public string Key { get; set; } = "";

    [Column("Value")]
    [NullSetting(NullSetting = NullSettings.Null)]
    [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
    public string? Value { get; set; }
}
