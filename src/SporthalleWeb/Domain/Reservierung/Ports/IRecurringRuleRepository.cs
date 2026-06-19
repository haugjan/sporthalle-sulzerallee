namespace SporthalleWeb.Domain.Reservierung.Ports;

public interface IRecurringRuleRepository
{
    Task<RecurringRule> SaveAsync(RecurringRule rule);
    Task<IReadOnlyList<RecurringRule>> GetActiveRulesAsync();
    Task<RecurringRule?> FindByIdAsync(int id);
    Task DeactivateAsync(int id);
}
