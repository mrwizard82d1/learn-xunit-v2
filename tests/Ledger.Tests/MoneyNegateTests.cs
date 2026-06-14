namespace Ledger.Tests;

public class MoneyNegateTests
{
    [Fact]
    public void SmokeTests()
    {
        Assert.Equal(4, 2 + 2);
    }

    [Theory]
    [InlineData(940.95, "MNT", "MNT")]
    [InlineData(-859.95, "bdT", "BDT")]
    [InlineData(0.00, "gmd", "GMD")]
    public void Negate_WithMoney_ReturnsMoneyWithNegativeAmountAndSameCurrency(decimal amount,
                                                                               string actualCurrency,
                                                                               string expectedCurrency)
    {
        var toTest = new Money(amount, new CurrencyCode(actualCurrency));
        var negated = toTest.Negate();

        Assert.Multiple(
            () => Assert.Equal(-amount, negated.Amount),
            () => Assert.Equal(expectedCurrency, negated.Currency.Value)
        );
    }

    [Fact]
    public void NegateNegate_WithMoney_EqualsOriginal()
    {
        var value = new Money(224.62M, new CurrencyCode("SAR"));

        Assert.Equal(new Money(224.62M, new CurrencyCode("SAR")), value.Negate().Negate());
    }
}