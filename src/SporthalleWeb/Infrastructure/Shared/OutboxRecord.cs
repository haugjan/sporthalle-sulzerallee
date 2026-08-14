using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace SporthalleWeb.Infrastructure.Shared;

[TableName("OutboxEmails")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class OutboxEmailRecord
{
    [Column("Id")] [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)] public int Id { get; set; }
    [Column("FromEmail")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(200)] public string FromEmail { get; set; } = "";
    [Column("FromName")] [NullSetting(NullSetting = NullSettings.Null)] [Length(200)] public string? FromName { get; set; }
    [Column("ToEmail")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(200)] public string ToEmail { get; set; } = "";
    [Column("ToName")] [NullSetting(NullSetting = NullSettings.Null)] [Length(200)] public string? ToName { get; set; }
    [Column("BccEmail")] [NullSetting(NullSetting = NullSettings.Null)] [Length(200)] public string? BccEmail { get; set; }
    [Column("Subject")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(300)] public string Subject { get; set; } = "";
    [Column("BodyHtml")] [NullSetting(NullSetting = NullSettings.NotNull)] [SpecialDbType(SpecialDbTypes.NTEXT)] public string BodyHtml { get; set; } = "";
    [Column("Status")] [NullSetting(NullSetting = NullSettings.NotNull)] [Index(IndexTypes.NonClustered)] public int Status { get; set; }
    [Column("Attempts")] [NullSetting(NullSetting = NullSettings.NotNull)] public int Attempts { get; set; }
    [Column("SentVia")] [NullSetting(NullSetting = NullSettings.Null)] [Length(200)] public string? SentVia { get; set; }
    [Column("LastError")] [NullSetting(NullSetting = NullSettings.Null)] [Length(1000)] public string? LastError { get; set; }
    [Column("Source")] [NullSetting(NullSetting = NullSettings.Null)] [Length(100)] public string? Source { get; set; }
    [Column("Reference")] [NullSetting(NullSetting = NullSettings.Null)] [Length(100)] public string? Reference { get; set; }
    [Column("CreatedAt")] [NullSetting(NullSetting = NullSettings.NotNull)] public DateTime CreatedAt { get; set; }
    [Column("NextAttemptAt")] [NullSetting(NullSetting = NullSettings.NotNull)] [Index(IndexTypes.NonClustered)] public DateTime NextAttemptAt { get; set; }
    [Column("SentAt")] [NullSetting(NullSetting = NullSettings.Null)] public DateTime? SentAt { get; set; }
}
