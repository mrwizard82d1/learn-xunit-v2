namespace Ledger;

public record CurrencyCode(string Value)
{
    public string Value { get; } = Normalize(Value);

    public override string ToString() => Value;

    private static string Normalize(string value)
    {
        if (value is null)
        {
            throw new ArgumentException("Currency code cannot be null.", nameof(value));
        }

        var trimmed = value.Trim();
        if (trimmed.Length != 3)
        {
            throw new ArgumentException(
                $"Currency code must be exactly 3 characters; got '{value}'.", nameof(value)
            );
        }

        if (!trimmed.All(char.IsLetter))
        {
            throw new ArgumentException(
                $"Currency code must contain only letters; got '{value}'.", nameof(value)
            );
        }
        
        return trimmed.ToUpperInvariant();
    }
}