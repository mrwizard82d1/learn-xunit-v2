namespace Ledger;

public class AccountRepository
{
    private readonly Dictionary<string, Account> _accounts = new();
    private int _nextId = 1;
    
    public bool Contains(string candidateId) => _accounts.ContainsKey(candidateId);
    
    public Account OpenAccount(Money initialBalance)
    { 
        var newAccountNumber = $"acc-{_nextId++}";
        var newAccount = new Account(newAccountNumber, initialBalance);
        _accounts.Add(newAccountNumber, newAccount);
        
        return newAccount;
    }

    public Account Get(string id)
    {
        return _accounts[id];
    }
}