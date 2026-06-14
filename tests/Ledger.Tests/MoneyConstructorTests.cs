namespace Ledger.Tests;

public class MoneyConstructorTests
{
    [Fact]
    public void SmokeTests()
    {
        Assert.Equal(4, 2 + 2);
    }

    [Theory]
    [InlineData(199.45)]
    [InlineData(-439.45)]
    [InlineData(0.00)]
    public void Construct_WithAmount_MoneyHasAmount(decimal amount)
    {
        var money = new Money(amount, new CurrencyCode("BBD"));

        Assert.Equal(amount, money.Amount);
    }

    [Theory]
    [InlineData("BBD", "BBD")]
    [InlineData("idR", "IDR")]
    [InlineData("gbp", "GBP")]
    [InlineData(" tTD", "TTD")]
    [InlineData(" Mnt\t", "MNT")]
    public void Construct_WithCurrency_MoneyHasNormalizedCurrency(string actualCurrency, string expectedCurrency)
    {
        var money = new Money(849.92M, new CurrencyCode(actualCurrency));

        Assert.Equal(expectedCurrency, money.Currency.Value);
    }
}
