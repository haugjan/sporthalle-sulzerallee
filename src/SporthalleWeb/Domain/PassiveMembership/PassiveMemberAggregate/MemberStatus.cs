namespace SporthalleWeb.Domain.PassiveMembership.PassiveMemberAggregate;

public enum MemberStatusValue { Pending, Confirmed, Deleted }

public record MemberStatus
{
    public static readonly MemberStatus Pending   = new(MemberStatusValue.Pending);
    public static readonly MemberStatus Confirmed = new(MemberStatusValue.Confirmed);
    public static readonly MemberStatus Deleted   = new(MemberStatusValue.Deleted);

    public MemberStatusValue Value { get; }

    /// <summary>Persisted string (dropdown value): "Pending" / "Confirmed" / "Deleted".</summary>
    public string Key => Value.ToString();

    public MemberStatus(MemberStatusValue value) => Value = value;

    public static MemberStatus FromKey(string? key) =>
        Enum.TryParse<MemberStatusValue>(key, ignoreCase: true, out var v)
            ? new MemberStatus(v)
            : throw new DomainException($"Unbekannter Status: {key}");

    public override string ToString() => Key;
}
