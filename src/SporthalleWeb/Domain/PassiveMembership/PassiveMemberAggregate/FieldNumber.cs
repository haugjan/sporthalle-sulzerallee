namespace SporthalleWeb.Domain.PassiveMembership.PassiveMemberAggregate;

public record FieldNumber
{
    public int Value { get; }

    public FieldNumber(int value)
    {
        if (value < 1 || value > 300)
            throw new DomainException($"Feldnummer muss zwischen 1 und 300 liegen (war: {value}).");
        Value = value;
    }
}
