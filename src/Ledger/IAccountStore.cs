namespace Ledger;

public interface IAccountStore
{
    Task SaveAsync(Account account); // operation: no result -> Task
    Task<Account?> GetAsync(string id); // future: Account? -> Task<Account>
}

public sealed class InMemoryAccountStore : IAccountStore
{
    private readonly Dictionary<string, Account> _accounts = new();

    public async Task SaveAsync(Account account)
    {
        await Task.Yield();
        _accounts[account.Id] = account;
    }

    public async Task<Account?> GetAsync(string id)
    {
        await Task.Yield();
        return _accounts.GetValueOrDefault(id);
    }
}