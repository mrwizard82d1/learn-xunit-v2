namespace Ledger.Tests;

public class AccountRepositoryTests
{
    private readonly AccountRepository _repository = new AccountRepository();

    [Fact]
    public void SmokeTest()
    {
        Assert.Equal(4, 2 + 2);
    }

    [Fact]
    public void OpenAccount_NoAccounts_AccountWithNewAccountIdExists()
    {
        var initialBalance = new Money(508.84M, new CurrencyCode("eur"));

        var newAccount = _repository.OpenAccount(initialBalance);

        Assert.True(_repository.Contains(newAccount.Id));
    }

    [Fact]
    public void OpenAccountWithInitialBalance_NoAccounts_OpenedAccountHasInitialBalance()
    {
        var initialBalance = new Money(820.49M, new CurrencyCode("kmf"));

        var newAccount = _repository.OpenAccount(initialBalance);

        Assert.Equal(new Money(820.49M, new CurrencyCode ("kmf")), newAccount.Balance);
    }

    [Fact]
    public void OpenAccount_OneAccountExists_ReturnsDifferentAccounts()
    {
        var firstAccountInitialBalance = new Money(973.85M, new CurrencyCode("mdl"));
        var firstAccount = _repository.OpenAccount(firstAccountInitialBalance);

        var secondAccountInitialBalance = new Money(825.98M, new CurrencyCode("myr"));
        var secondAccount = _repository.OpenAccount(secondAccountInitialBalance);

        Assert.NotEqual(firstAccount, secondAccount);
    }

    [Fact]
    public void OpenAccount_OneAccountExists_ReturnsAccountWithDifferentId()
    {
        var firstAccountInitialBalance = new Money(768.77M, new CurrencyCode("tmt"));
        var firstAccount = _repository.OpenAccount(firstAccountInitialBalance);

        var secondAccountInitialBalance = new Money(768.77M, new CurrencyCode("tmt"));
        var secondAccount = _repository.OpenAccount(secondAccountInitialBalance);

        Assert.NotEqual(firstAccount.Id, secondAccount.Id);
    }

    [Fact]
    public void OpenAccount_OneAccountExists_TwoAccountsWithDifferentIdsExist()
    {
        var firstAccountInitialBalance = new Money(848.19M, new CurrencyCode("lsl"));
        var firstAccount = _repository.OpenAccount(firstAccountInitialBalance);

        var secondAccountInitialBalance = new Money(402.84M, new CurrencyCode("xdr"));
        var secondAccount = _repository.OpenAccount(secondAccountInitialBalance);

        Assert.Multiple(
            () => Assert.True(_repository.Contains(firstAccount.Id)),
            () => Assert.True(_repository.Contains(secondAccount.Id))
        );
    }

    [Fact]
    public void GetAccount_AccountWithIdExists_ReturnsAccount()
    {
        var initialBalance = new Money(848.19M, new CurrencyCode("lsl"));
        var addedAccount = _repository.OpenAccount(initialBalance);

        var foundAccount = _repository.Get(addedAccount.Id);
        Assert.Equal(addedAccount, foundAccount);
    }

    [Fact]
    public void Contains_AccountRepositoryDoesNotContainId_ReturnsFalse()
    {
        var initialBalance = new Money(986.66M, new CurrencyCode("sos"));
        _repository.OpenAccount(initialBalance);

        Assert.False(_repository.Contains("no-such-account"));
    }

    // "Faux" tests to verify that the instance member, `_repository``, is
    // created anew for each test instance or only once for all tests in the
    // class.
    //
    // In particular these two tests should fail if run sequentially (an
    // assumption) but `repository` is only initialized once.
    //
    // These tests are not needed to demonstrate the functionality of our
    // system under test; however, I have chosen to keep them for pedagogic
    // reasons.
    [Fact]
    public void Lifecycle_PartOne_OpensAccount()
    {
        var account = _repository.OpenAccount(new Money(508.47M, new CurrencyCode("ang")));
        Assert.True(_repository.Contains(account.Id));
    }

    [Fact]
    public void Lifecycle_PartTwo_RepositoryStartsFresh()
    {
        // If `PartOne` and `PartTwo` share the same `_repository` instance,
        // then repository counter would have advanced past "acc-1". If we get back
        // "acc-1", then each `[Fact]` is getting **its own** fresh instance of `AccountRepositoryTests`.
        var account = _repository.OpenAccount(new Money(508.74M, new CurrencyCode("ang")));
        Assert.Equal("acc-1", account.Id);
    }
}
