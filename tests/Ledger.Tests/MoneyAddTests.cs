namespace Ledger.Tests;

public class MoneyAddTests
{
    [Fact]
    public void SmokeTests()
    {
        Assert.Equal(4, 2 + 2);
    }

    [Fact]
    public void TwoMoneyInstances_Add_TypeOfReturnValueIsMoney()
    {
        var addend1 = new Money(269.84M, new CurrencyCode("TND"));
        var addend2 = new Money(530.90M, new CurrencyCode("TND"));

        Assert.IsType<Money>(addend1.Add(addend2));
    }

    public static TheoryData<decimal, decimal, decimal, CurrencyCode> AddCases =>
        new()
        {
            { 607.37M, 733.74M, 1341.11M, new CurrencyCode("DKK") },
            { 871.13M, -892.52M, -21.39M, new CurrencyCode("BSD") },
            { 0M, 913.38M, 913.38M, new CurrencyCode("IQD") },
        };

    [Theory]
    [MemberData(nameof(AddCases))]
    public void TwoMonies_Add_ProducesExpectedSum(decimal oneAmount, decimal anotherAmount, decimal expectedSum, 
    CurrencyCode currency)
    {
        var actual = new Money(oneAmount, currency).Add(new Money(anotherAmount, currency));
        
        Assert.Equal(new Money(expectedSum, currency), actual);
    }

    [Fact]
    public void TwoMoneyInstancesDifferentCurrencies_Add_ThrowsInvalidOperationException()
    {
        var addend1 = new Money(591.18M, new CurrencyCode("PHP"));
        var addend2 = new Money(268.73M, new CurrencyCode("PHO"));

        Assert.Throws<InvalidOperationException>(() => addend1.Add(addend2));
    }

    [Fact]
    public void TwoMoneyInstancesDifferentCurrencies_Add_ErrorMessageContainsCorrectCurrencyCodes()
    {
        var addend1 = new Money(391.76M, new CurrencyCode("RUB"));
        var addend2 = new Money(834.68M, new CurrencyCode("NIO"));

        var ex = Assert.Throws<InvalidOperationException>(() => addend1.Add(addend2));
        Assert.Multiple(
            () => Assert.Contains("RUB", ex.Message),
            () => Assert.Contains("NIO", ex.Message));
    }

    [Fact]
    public void TwoMoneyInstancesDifferentCurrencies_Add_ErrorMessageContainsOperation()
    {
        var addend1 = new Money(-282.55M,new CurrencyCode("QAR"));
        var addend2 = new Money(995.59M, new CurrencyCode("BOB"));

        var ex = Assert.Throws<InvalidOperationException>(() => addend1.Add(addend2));
        Assert.Matches(@"\badd\b", ex.Message);
    }
}
