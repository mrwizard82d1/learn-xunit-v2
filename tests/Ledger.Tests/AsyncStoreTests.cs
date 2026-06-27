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
    
    // DO NOT DO THIS!
    [Fact]
    public async void SaveThenGet_AsyncVoid_DoNotDoThis()
    {
        var store = new InMemoryAccountStore();
        await store.SaveAsync(new Account("acc-x", new Money(1M, new CurrencyCode("USD"))));
        Assert.NotNull(await store.GetAsync("acc-x"));
    }
}