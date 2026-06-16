namespace Ledger.Tests;

public class AccountScenarioTests
{
    private readonly ITestOutputHelper _output;

    public AccountScenarioTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public void SmokeTest() => Assert.Equal(4, 2 + 2);

    [Fact]
    public void OpenSeveralAccounts_QueryEach_AllPresent()
    {
        var repo = new AccountRepository();
        var kyd = new CurrencyCode("kyd");

        var checking = repo.OpenAccount(new Money(527.68M, kyd));
        _output.WriteLine($"Opened checking: {checking.Id}");

        var savings = repo.OpenAccount(new Money(836.31M, kyd));
        _output.WriteLine($"Opened savings: {savings.Id}");
     
        Assert.True(repo.Contains(checking.Id));
        Assert.True(repo.Contains(savings.Id));
        
        _output.WriteLine("Both created accounts verified present.");
    }
}