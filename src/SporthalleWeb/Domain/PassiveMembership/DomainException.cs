namespace SporthalleWeb.Domain.PassiveMembership;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

public sealed class FieldAlreadyTakenException : DomainException
{
    public FieldAlreadyTakenException(FieldNumber field)
        : base($"Feld Nr. {field.Value} ist bereits belegt.") { }
}

public sealed class MemberNotFoundException : DomainException
{
    public MemberNotFoundException(int id)
        : base($"Passivmitglied mit Id {id} wurde nicht gefunden.") { }
}
