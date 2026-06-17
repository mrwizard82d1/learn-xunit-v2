namespace Ledger.Tests;

public class SmokeTest
{
    [Fact]
    [Trait("Category", "Smoke")]
    public void ArithmeticSmoke()
    {
        Assert.Equal(4, 2 + 2);
    }
}