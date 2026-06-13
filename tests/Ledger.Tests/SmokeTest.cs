namespace Ledger.Tests;

public class SmokeTest
{
    [Fact]
    public void ArithmeticSmoke()
    {
        Assert.Equal(4, 2 + 2);
    }
}