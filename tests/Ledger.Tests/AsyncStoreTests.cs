namespace Ledger.Tests;

public class AsyncStoreTests
{
    [Fact]
    [Trait("Category", "Smoke")]
    public void SmokeTest() => Assert.Equal(4, 2 + 2);

    [Fact]
    public async Task SaveThenGetAsync_ReturnsSameAccount()
    {
        var store = new InMemoryAccountStore();
        var account = new Account("acc-1", new Money(170.09M, new CurrencyCode("GBP")));

        await store.SaveAsync(account);
        var fetched = await store.GetAsync("acc-1");
        
        Assert.Equal(account, fetched);
    }

    [Fact]
    public async Task GetRequiredAsync_MissingId_ThrowsAsync()
    {
        var store = new InMemoryAccountStore();
        
        await Assert.ThrowsAsync<KeyNotFoundException>(() => store.GetRequiredAsync("nope"));
    }
}