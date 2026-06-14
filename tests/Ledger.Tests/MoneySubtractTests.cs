namespace Ledger.Tests;

public class MoneySubtractTests
{
    [Fact]
    public void SmokeTests()
    {
        Assert.Equal(4, 2 + 2);
    }

    [Fact]
    public void TwoMoneyInstances_Subtract_DifferenceIsCorrect()
    {
        var minuend = new Money(273.03M, new CurrencyCode("BDT"));
        var subtrahend = new Money(282.35M, new CurrencyCode("BDT"));

        Assert.Equal(new Money(-9.32M, new CurrencyCode("BDT")), minuend.Subtract(subtrahend));
    }

    [Fact]
    public void TwoMoneyInstances_Subtract_CurrencyIsSameAsCurrencyOfFirst()
    {
        var minuend = new Money(-345.21M, new CurrencyCode("EUR"));
        var subtrahend = new Money(268.54M, new CurrencyCode("EUR"));

        Assert.Equal(new Money(-613.75M, new CurrencyCode("EUR")), minuend.Subtract(subtrahend));
    }

    [Fact]
    public void TwoMoneyInstancesDifferentCurrencies_Subtract_ThrowsInvalidOperationException()
    {
        var minuend = new Money(157.58M, new CurrencyCode("EGP"));
        var subtrahend = new Money(137.06M, new CurrencyCode("EGR"));

        Assert.Throws<InvalidOperationException>(() => minuend.Subtract(subtrahend));
    }

    [Fact]
    public void TwoMoneyInstancesDifferentCurrencies_Subtract_ErrorMessageStartsWithCorrectText()
    {
        var minuend = new Money(-852.19M, new CurrencyCode("SCR"));
        var subtrahend = new Money(288.67M, new CurrencyCode("CUC"));

        var ex = Assert.Throws<InvalidOperationException>(() => minuend.Subtract(subtrahend));
        Assert.Matches(@"\bsubtract\b", ex.Message);
    }
}
