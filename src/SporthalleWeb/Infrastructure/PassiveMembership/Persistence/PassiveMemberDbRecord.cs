using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace SporthalleWeb.Infrastructure.PassiveMembership.Persistence;

[TableName("PassivMitglieder")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class PassiveMemberDbRecord
{
    [Column("Id")]
    public int Id { get; set; }

    [Column("FieldNumber")]
    public int FieldNumber { get; set; }

    [Column("FirstName")]
    [Length(100)]
    public string FirstName { get; set; } = "";

    [Column("LastName")]
    [Length(100)]
    public string LastName { get; set; } = "";

    [Column("AddressLine")]
    [Length(300)]
    public string AddressLine { get; set; } = "";

    [Column("AddressLine2")]
    [Length(300)]
    [NullSetting(NullSetting = NullSettings.Null)]
    public string? AddressLine2 { get; set; }

    [Column("PostalCode")]
    [Length(20)]
    public string PostalCode { get; set; } = "";

    [Column("City")]
    [Length(100)]
    public string City { get; set; } = "";

    [Column("Country")]
    [Length(100)]
    public string Country { get; set; } = "Schweiz";

    [Column("Phone")]
    [Length(50)]
    [NullSetting(NullSetting = NullSettings.Null)]
    public string? Phone { get; set; }

    [Column("Email")]
    [Length(200)]
    public string Email { get; set; } = "";

    [Column("MembershipLevel")]
    [Length(20)]
    public string MembershipLevel { get; set; } = "";

    [Column("ShowNameOnFloor")]
    public bool ShowNameOnFloor { get; set; }

    [Column("DisplayName")]
    [Length(200)]
    [NullSetting(NullSetting = NullSettings.Null)]
    public string? DisplayName { get; set; }

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; }

    [Column("Status")]
    [Length(20)]
    public string Status { get; set; } = "Confirmed";

    [Column("ConfirmedAt")]
    [NullSetting(NullSetting = NullSettings.Null)]
    public DateTime? ConfirmedAt { get; set; }

    [Column("ConfirmedBy")]
    [Length(200)]
    [NullSetting(NullSetting = NullSettings.Null)]
    public string? ConfirmedBy { get; set; }

    [Column("PaidAt")]
    [NullSetting(NullSetting = NullSettings.Null)]
    public DateTime? PaidAt { get; set; }

    [Column("PaidBy")]
    [Length(200)]
    [NullSetting(NullSetting = NullSettings.Null)]
    public string? PaidBy { get; set; }

    [Column("ExportedToAccounting")]
    public bool ExportedToAccounting { get; set; }

    [Column("ExportedToAccountingAt")]
    [NullSetting(NullSetting = NullSettings.Null)]
    public DateTime? ExportedToAccountingAt { get; set; }

    [Column("ExportedToAccountingBy")]
    [Length(200)]
    [NullSetting(NullSetting = NullSettings.Null)]
    public string? ExportedToAccountingBy { get; set; }

    [Column("Notes")]
    [NullSetting(NullSetting = NullSettings.Null)]
    [Length(4000)]
    public string? Notes { get; set; }
}
