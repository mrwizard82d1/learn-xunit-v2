namespace Ledger.Tests;

public class MoneyAddTests
{
    [Fact]
    public void SmokeTest()
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

    // About to make a v2-specific fix since v2 unit tests **do not** correctly
    // handle a `CurrencyCode` argument - in a very subtle way.
    //
    // Code using `xUnit` v3 correctly handles a type like `CurrencyCode` as an
    // argument to the templated `TheoryData<>`. Code using `xUnit` v2
    // **does not** handle this scenario correctly **when run using `dotnet`**.
    // Running these tests using `dotnet test...`  displays a warning about its
    // inability to serialize instances of `CurrencyCode` and then prints the 
    // test results of this test as a **single test**. Rider and `xUnit` v3
    // correctly recognize the three tests under execution.
    //
    // The "fix" for `xUnit` v2 is to only pass arguments of primitive types.
    public static TheoryData<decimal, decimal, decimal, string> AddCases =>
        new()
        {
            { 607.37M, 733.74M, 1341.11M, "DKK" },
            { 871.13M, -892.52M, -21.39M, "BSD" },
            { 0M, 913.38M, 913.38M, "IQD" },
        };

    [Theory]
    [MemberData(nameof(AddCases))]
    public void TwoMonies_Add_ProducesExpectedSum(decimal oneAmount, decimal anotherAmount, decimal expectedSum, 
    string currencyText)
    {
        var actual = new Money(oneAmount, 
                               new CurrencyCode(currencyText)).Add(new Money(anotherAmount, 
                                                                             new CurrencyCode(currencyText)));
        
        Assert.Equal(new Money(expectedSum, new CurrencyCode(currencyText)), actual);
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
