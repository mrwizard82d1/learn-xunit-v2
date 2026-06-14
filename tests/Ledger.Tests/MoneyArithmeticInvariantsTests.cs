namespace Ledger.Tests;

// Candidate tests:
//
public class MoneyArithmeticInvariantsTests
{
    [Fact]
    public void SmokeTests()
    {
        Assert.Equal(4, 2 + 2);
    }

    // Identity element

    [Fact]
    public void TwoMoneyInstancesButSecondZero_Add_SumEqualsFirstArgument()
    {
        var addend1 = new Money(-591.35M, new CurrencyCode("GGP"));
        var addend2 = new Money(0M, new CurrencyCode("GGP"));

        Assert.Equal(new Money(-591.35M, new CurrencyCode("GGP")), addend1.Add(addend2));
    }

    [Fact]
    public void TwoMoneyInstancesButFirstZero_Add_SumEqualsSecondArgument()
    {
        var addend1 = new Money(0M, new CurrencyCode("GGP"));
        var addend2 = new Money(-591.35M, new CurrencyCode("GGP"));

        Assert.Equal(new Money(-591.35M, new CurrencyCode("GGP")), addend1.Add(addend2));
    }

    // Commutativity

    [Fact]
    public void TwoMoneyInstances_Add_Commutes()
    {
        var addend1 = new Money(354.49M, new CurrencyCode("CHF"));
        var addend2 = new Money(765.75M, new CurrencyCode("CHF"));

        Assert.Equal(addend1.Add(addend2), addend2.Add(addend1));
    }

    // Associativity

    [Fact]
    public void ThreeMoneyInstances_Add_Associates()
    {
        var addend1 = new Money(-502.87M, new CurrencyCode("lrd"));
        var addend2 = new Money(-309.46M, new CurrencyCode("Lrd"));
        var addend3 = new Money(686.38M, new CurrencyCode("lrD"));

        Assert.Equal(addend1.Add(addend2.Add(addend3)), (addend1.Add(addend2)).Add(addend3));
    }

    // Subtraction is identical to adding the negative

    [Fact]
    public void TwoMoneyInstances_Subtract_EqualToAdditionOfNegative()
    {
        var minuend = new Money(-316.43M, new CurrencyCode("PYG"));
        var subtrahend = new Money(-835.06M, new CurrencyCode("PYG"));
        var negativeSubtrahend = new Money(835.06M, new CurrencyCode("PYG"));

        Assert.Equal(minuend.Subtract(subtrahend), minuend.Add(negativeSubtrahend));
    }
}
