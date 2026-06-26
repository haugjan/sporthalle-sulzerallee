namespace SporthalleWeb.Domain.PassiveMembership;

public class DomainException(string message) : Exception(message);

public sealed class FieldAlreadyTakenException(FieldNumber field)
    : DomainException($"Feld Nr. {field.Value} ist bereits belegt.");

public sealed class MemberNotFoundException(int id)
    : DomainException($"Passivmitglied mit Id {id} wurde nicht gefunden.");
