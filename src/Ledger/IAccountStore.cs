namespace Ledger;

public interface IAccountStore
{
    Task SaveAsync(Account account); // operation: no result -> Task
    Task<Account?> GetAsync(string id); // future: Account? -> Task<Account>

    // An operation that throws an exception from an async operation.
    Task<Account> GetRequiredAsync(string id);
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

    public async Task<Account> GetRequiredAsync(string id)
    {
        await Task.Yield();
        return (_accounts.TryGetValue(id, out var account)
                    ? account
                    : throw new KeyNotFoundException($"No account '{id}'."));
    }
}