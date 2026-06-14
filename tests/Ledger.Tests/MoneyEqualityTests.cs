namespace Ledger.Tests;

public class MoneyEqualityTests
{
    [Fact]
    public void SmokeTests()
    {
        Assert.Equal(4, 2 + 2);
    }

    [Fact]
    public void InstancesWithEqualCurrencyAndAmountAreEqual()
    {
        var actual = new Money(778.72M, new CurrencyCode("CAD"));
        var expect = new Money(778.72M, new CurrencyCode("CAD"));

        Assert.Equal(expect, actual);
    }

    [Fact]
    public void InstancesWithSameAmountButDifferentCurrencyAreNotEqual()
    {
        var actual = new Money(778.72M, new CurrencyCode("CAD"));
        var expect = new Money(778.72M, new CurrencyCode("CAE"));

        Assert.NotEqual(expect, actual);
    }

    [Fact]
    public void InstancesWithDifferentAmountsButSameCurrencyAreNotEqual()
    {
        var actual = new Money(778.72M, new CurrencyCode("CAD"));
        var expect = new Money(778.71M, new CurrencyCode("CAD"));

        Assert.NotEqual(expect, actual);
    }

    [Fact]
    public void AnInstanceAndAnAlias_ReportTheSame()
    {
        var actual = new Money(-115.63M, new CurrencyCode("IRR"));
        // ReSharper disable once InlineTemporaryVariable
        var alias = actual;

        Assert.Same(alias, actual);
    }

    [Fact]
    public void TwoEqualInstances_DoNotReportTheSame()
    {
        var someMoney = new Money(-975.29M, new CurrencyCode("MYR"));
        var equalMoney = new Money(-975.29M, new CurrencyCode("MYR"));

        Assert.NotSame(someMoney, equalMoney);
    }
}
