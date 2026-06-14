using Ledger.Tests.Fixtures;

namespace Ledger.Tests;

[Collection("Seeded accounts")]
public class AccountQueryTests
{
    private readonly SeededAccountsFixture _fixture;

    // ReSharper disable once ConvertToPrimaryConstructor
    public AccountQueryTests(SeededAccountsFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void SmokeTest() => Assert.Equal(4, 2 + 2);

    [Fact]
    public void Get_KnownAccountId_ReturnsAccount()
    {
        var fetched = _fixture.Repository.Get(_fixture.Checking.Id);
        
        Assert.Equal(_fixture.Checking, fetched);
    }

    [Fact]
    public void Contains_KnownAccountId_ReturnsTrue()
    {
        Assert.True(_fixture.Repository.Contains(_fixture.Savings.Id));
    }

    [Fact]
    public void LifeCycle_PartOne_RecordsFixtureInstanceId()
    {
        // No assertion. This test exists merely to demonstrate that the fixture
        // is shared with the `PartTwo` test.
        Assert.NotEqual(Guid.Empty, _fixture.InstanceId);
    }

    [Fact]
    public void Lifecycle_PartTwo_SeesSameFixtureInstance()
    {
        // This test is meaningful only in combination with `PartOne`: both
        // tests received the **same** fixture (and therefore the same
        // `InstanceId`), because `xUnit` instantiated `SeededAccountFixture`
        // exactly once for the class.
        Assert.NotEqual(Guid.Empty, _fixture.InstanceId);
    }
}
