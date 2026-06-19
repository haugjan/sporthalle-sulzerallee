namespace SporthalleWeb.Domain.PassivMitgliedschaft;

public record MembershipLevel
{
    public static readonly MembershipLevel Bronze = new("Hallenbodenbesitzer", "Bronze", 50);
    public static readonly MembershipLevel Silber = new("Chnebler", "Silber", 100);
    public static readonly MembershipLevel Gold   = new("Cüpli-Chnebler", "Gold", 200);

    public string DisplayName { get; }
    public string Key { get; }
    public decimal YearlyFee { get; }

    private MembershipLevel(string displayName, string key, decimal fee)
    {
        DisplayName = displayName;
        Key = key;
        YearlyFee = fee;
    }

    public static MembershipLevel FromKey(string key) => key switch
    {
        "Bronze" => Bronze,
        "Silber" => Silber,
        "Gold"   => Gold,
        _        => throw new DomainException($"Unbekannte Mitgliedsstufe: {key}")
    };
}
