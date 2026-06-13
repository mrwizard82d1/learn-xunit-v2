
namespace Ledger;

public record Money(decimal Amount, CurrencyCode Currency)
{
    public Money Add(Money addend2)
    {
        EnsureSameCurrency(addend2, "add");
        return new Money(Amount + addend2.Amount, Currency);
    }

    public Money Subtract(Money subtrahend)
    {
        EnsureSameCurrency(subtrahend, "subtract");
        return new Money(Amount - subtrahend.Amount, Currency);
    }

    private void EnsureSameCurrency(Money instance, string operation)
    {
        if (Currency != instance.Currency)
        {
            throw new InvalidOperationException($"Cannot {operation}: {Currency} != {instance.Currency}");
        }
    }

    public Money Negate()
    {
        return new Money(-Amount, Currency);
    }
}