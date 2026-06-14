namespace Ledger.Tests.Fixtures;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class SeededAccountsFixture
{
    // Create an instance initialized when created. Prove that initialization 
    // occurs exactly **once**.
    public Guid InstanceId { get; } = Guid.NewGuid();
    
    // The `Repository` member is public. This choice is more pedagogical than
    // required. One could probably encapsulate `Repository` and complete the
    // tutorial with minor modifications. I've chosen to leave it `public`
    // simply to move on with the goal: learning `xUnit`. This comment is a 
    // reminder to "future me" that it may not be the most robust
    // implementation.
    // ReSharper disable once MemberCanBePrivate.Global
    public AccountRepository Repository { get; }
    public Account Checking { get; }
    public Account Savings { get; }
    public Account Empty { get; }

    public SeededAccountsFixture()
    {
        Repository = new AccountRepository();
        Checking = Repository.OpenAccount(new Money(270.95M, new CurrencyCode("IRR")));
        Savings = Repository.OpenAccount(new Money(336.20M, new CurrencyCode("IRR")));
        Empty = Repository.OpenAccount(new Money(0M, new CurrencyCode("IRR")));
    }
}

[CollectionDefinition("Seeded accounts")]
public class SeededAccountsCollection : ICollectionFixture<SeededAccountsFixture>
{
    // Intentionally empty. The marker exists only so xUnit can find the
    // `[[CollectionDefinition]]` and its `ICollectionFixture<T>` declaration. 
}

// ReSharper disable once ClassNeverInstantiated.Global
[Collection("Seeded accounts")]
public class AccountInventoryTests
{
    private readonly SeededAccountsFixture _fixture;

    // ReSharper disable once ConvertToPrimaryConstructor
    public AccountInventoryTests(SeededAccountsFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void SmokeTest() => Assert.Equal(4, 2 + 2);

    [Fact]
    public void Inventory_AfterSeeding_ContainsThreeAccounts()
    {
        Assert.True(_fixture.Repository.Contains(_fixture.Checking.Id)); 
        Assert.True(_fixture.Repository.Contains(_fixture.Savings.Id)); 
        Assert.True(_fixture.Repository.Contains(_fixture.Empty.Id)); 
    }
}
