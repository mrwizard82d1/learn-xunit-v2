# Phase 6 — Async tests done right (v2)

> Summary in [`../TUTORIAL_PLAN.md`](../TUTORIAL_PLAN.md). This is the detailed walkthrough. **Fresh material** — the v3 tutorial never reached Phase 6, so there's nothing to port; we drive this in v2 idiom. Expect `v3 ↔ v2` call-outs, because async is one of the areas v2 and v3 actually differ in signatures.

## How to use this file

- Step headers end with `` `[ ]` ``. Flip to `` `[x]` `` when complete (you do this at your commit step).
- First unchecked step = your resume point.
- "Notes & questions" at the bottom is yours.
- **Test runs:** `Ledger.Tests` Run configuration (Rider) or `dotnet test` (CLI).

## Goal

Learn to test asynchronous code *honestly* in xUnit v2 — where "honestly" means the runner actually awaits your test and reports real pass/fail, rather than a test that returns early and lies. Specifically:

- **`async Task` facts/theories** — the correct async test signature, and how xUnit awaits them.
- **The `async void` trap** — why it's dangerous, why v2 *runs it anyway* (and v3 won't).
- **Async exceptions** — `await Assert.ThrowsAsync<T>(…)` (the sync `Assert.Throws` silently does the wrong thing on a `Task`).
- **`IAsyncLifetime`** — async setup/teardown, the async counterpart to Phase 2's constructor/`Dispose` and Phase 4's fixtures.

Our domain is entirely synchronous, so async tests have nothing to `await`. Step 2 introduces a deliberately minimal **async seam** (`IAccountStore` + `InMemoryAccountStore`) as a stand-in for a real I/O boundary (DB/HTTP). It's clearly labeled as a stand-in and is reused as the integration-testing vehicle in Phase 9.

## `v3 ↔ v2` orientation (async is a real signature divergence)

Two differences matter here, both verified against xUnit docs:

1. **`IAsyncLifetime` return types.** v2: `Task InitializeAsync()` and `Task DisposeAsync()`. v3: both return **`ValueTask`**, and `IAsyncLifetime` inherits `IAsyncDisposable`. So v2 code that implements `IAsyncLifetime` won't compile unchanged on v3 — the signatures change. We write the **v2 (`Task`) form** here.
2. **`async void` tests.** v2 *supports* them — it installs an `AsyncTestSyncContext` that tracks the async-void completion, so they actually run. v3 **removed** that and **fast-fails** any `async void` test at runtime. So v2 quietly tolerates a footgun; the correct answer in both is `async Task`.

(`NUnit ↔ xUnit`: NUnit also runs `async Task` tests and discourages `async void`. NUnit's async `[SetUp]`/`[TearDown]` (Task-returning) map to xUnit's `IAsyncLifetime.InitializeAsync`/`DisposeAsync`. NUnit has no per-test-instance reconstruction, so its setup story differs — see Phase 2.)

> **Background reading (optional aside):** for the bigger picture — C#'s road to `async/await`, F#'s cold `Async` workflows (which predated and inspired it), `task { }`, and the actor model / `MailboxProcessor` — see [`../notes/async-and-concurrency-csharp-vs-fsharp.md`](../notes/async-and-concurrency-csharp-vs-fsharp.md). Not needed to do this phase; it's a durable pointer to the shape of the space.

## Decisions made

- (inherits all prior phase patterns: smoke-as-canary, TDD red-green, `CurrencyCode`, fixtures, traits.)
- **Introduce a minimal async seam** (`IAccountStore` + `InMemoryAccountStore`) rather than fake async on the sync `AccountRepository`. The in-memory impl uses `await Task.Yield()` at the boundary to be *genuinely* asynchronous (forces a continuation) rather than sync-over-`Task` (`Task.FromResult`) — so the tests actually exercise awaiting. Labeled a stand-in for real I/O; **kept** (reused in Phase 9). *(Revisit if it feels like scope creep — alternative is a thin async service wrapper over the existing repo.)*
- **Write the v2 `IAsyncLifetime` (`Task`) form.** *(add package/version decisions if any arise — none expected; async needs no extra packages.)*

## Candidate test list (Kent Beck style — check off / edit as we go)

- [x] `AsyncStoreTests.SmokeTest` — canary for the new class
- [ ] `SaveThenGetAsync_ReturnsSameAccount` — basic `async Task` fact
- [ ] (observe) an `async void` version — see v2 run it, note v3 wouldn't
- [ ] `GetRequiredAsync_MissingId_ThrowsAsync` — `Assert.ThrowsAsync<KeyNotFoundException>`
- [ ] `AsyncSeededStoreFixture` (`IAsyncLifetime`) seeds via `await SaveAsync(...)`; prove `InitializeAsync` runs **once**
- [ ] (stretch) an `async Task` `[Theory]` with `[InlineData]`

---

## Steps

### Step 1 — Per-class smoke for `AsyncStoreTests`  `[x]`

Canary first, per the standing pattern. Create `tests/Ledger.Tests/AsyncStoreTests.cs`:

```csharp
namespace Ledger.Tests;

public class AsyncStoreTests
{
    [Fact]
    [Trait("Category", "Smoke")]
    public void SmokeTest() => Assert.Equal(4, 2 + 2);
}
```

Run `dotnet test`. One new green (suite 74 → 75; smoke filter 12 → 13). Tagging the smoke keeps the canary set complete (Phase 5 Step 4 convention).

### Step 2 — Introduce the async seam and the first `async Task` fact  `[ ]`

TDD, and mind the "compile errors aren't valid red" rule: create the production types *first* (so it compiles), but leave the impl returning a wrong/empty result so the **assertion** fails as a real red.

Production — `src/Ledger/IAccountStore.cs`:

```csharp
namespace Ledger;

public interface IAccountStore
{
    Task SaveAsync(Account account);
    Task<Account?> GetAsync(string id);
}

public sealed class InMemoryAccountStore : IAccountStore
{
    private readonly Dictionary<string, Account> _accounts = new();

    public async Task SaveAsync(Account account)
    {
        await Task.Yield();                 // stand-in for a real async I/O boundary
        _accounts[account.Id] = account;
    }

    public async Task<Account?> GetAsync(string id)
    {
        await Task.Yield();
        return _accounts.TryGetValue(id, out var account) ? account : null;
    }
}
```

> **Why `Task.Yield()` and not `Task.FromResult(...)`?** `Task.FromResult` is *sync-over-Task* — the method returns an already-completed task and never actually yields, so an `await` on it runs synchronously and the test never exercises a real continuation. `await Task.Yield()` forces the method to suspend and resume on a continuation, so the async machinery is genuinely engaged. For a stand-in, that's the honest choice.

Test — in `AsyncStoreTests`, the basic `async Task` fact:

```csharp
[Fact]
public async Task SaveThenGetAsync_ReturnsSameAccount()
{
    var store = new InMemoryAccountStore();
    var account = new Account("acc-1", new Money(100M, new CurrencyCode("USD")));

    await store.SaveAsync(account);
    var fetched = await store.GetAsync("acc-1");

    Assert.Equal(account, fetched);
}
```

The signature is **`async Task`**, not `async void`. xUnit sees the returned `Task`, awaits it, and only then judges pass/fail — so the assertion after the `await` is actually observed.

To see a real red first (optional but in the spirit): stub `GetAsync` to `return null;` (after the `await`), watch the test fail on the assertion, then restore the real lookup → green.

> **A `Task` is *hot*, not a thunk — and the compiler, not the name, enforces async.** Worth pinning down, because it's the conceptual heart of async:
>
> - **Awaitable vs. required.** `Task`/`Task<T>` are *awaitable* (they have `GetAwaiter()`), but awaiting is **not required** by the compiler — you may ignore a Task, stash it, await it later, or never (an unawaited async call inside an `async` method warns: CS4014). Awaiting is how you get the *result*, observe *exceptions*, and *sequence* — not a syntax rule.
> - **The compiler enforces async via the *type*, not the name.** `var x = store.GetAsync(id);` makes `x` a `Task<Account?>` — **not** an `Account?`. Use `x` as an `Account?` and it won't compile; you're forced to `await` (or `.Result`) to unwrap. Rename `GetAsync` → `DoInterestingStuff` and the rule is identical — the `Async` suffix is a *readability convention for humans*, never the enforcement mechanism. So an interface returning `Task<T>` is an honest abstraction barrier at the *type* level: it hides the *mechanism* (real async vs. sync-over-Task vs. CPU work) but cannot hide the *shape*.
> - **Hot, not a thunk.** A *thunk* (Lisp `(fn [] …)`, Clojure `delay`, F# `Async<'T>`) is **cold/deferred** — nothing runs until you force it. A C# `Task` is the **opposite — hot/eager**: it represents an operation *already running or finished* the moment it exists. `await` doesn't *start* it; it *waits for* it. So "create a Task that doesn't need awaiting" is a category error — every Task can be *ignored*, but you can't get its value as a `T` without `await`/`.Result`, because its *type* is `Task<T>`.
> - **If you want cold (a value computed on demand, no await ceremony), you don't want a `Task`:** `T` (eager value), `Lazy<T>` (lazy, memoized), `Func<T>` (a thunk), `Func<Task<T>>` (a *deferred* async op — a thunk that starts a Task when invoked), or F#'s native cold `Async<'T>`. The bridge is symmetric: **cold = `() => hot`** (wrap a hot Task in a function to defer it), **hot = `run(cold)`**.
>
> See [`../notes/async-and-concurrency-csharp-vs-fsharp.md`](../notes/async-and-concurrency-csharp-vs-fsharp.md) → *"Hot vs cold: futures across languages"* for how other languages choose hot or cold by default.

### Step 3 — The `async void` trap (v2 runs it; v3 wouldn't)  `[ ]`

Temporarily add an `async void` version to feel the difference:

```csharp
[Fact]
public async void SaveThenGet_AsyncVoid_DoNotDoThis()
{
    var store = new InMemoryAccountStore();
    await store.SaveAsync(new Account("acc-x", new Money(1M, new CurrencyCode("USD"))));
    Assert.NotNull(await store.GetAsync("acc-x"));
}
```

Run it. **In v2 it actually passes** — xUnit's `AsyncTestSyncContext` tracks the async-void completion so the runner waits for it. That's the trap: it *looks* fine.

Why it's still wrong (and why v3 bans it):
- An `async void` method can't be awaited by *callers* — exceptions escape to the synchronization context rather than the awaiter. Outside xUnit's special handling, a failing assertion in an `async void` can crash the process or be swallowed.
- v3 **fast-fails** any `async void` test, forcing `async Task`. So an `async void` test that "works" in v2 becomes a hard error the moment you migrate.

**Then delete this method** (don't keep the anti-pattern in the suite). The lesson stays in this doc and your Notes; the code shouldn't. Net suite count returns to where Step 2 left it.

### Step 4 — Async exceptions with `Assert.ThrowsAsync<T>`  `[ ]`

Add a throwing async operation to the seam — `IAccountStore`:

```csharp
Task<Account> GetRequiredAsync(string id);
```

`InMemoryAccountStore`:

```csharp
public async Task<Account> GetRequiredAsync(string id)
{
    await Task.Yield();
    return _accounts.TryGetValue(id, out var account)
        ? account
        : throw new KeyNotFoundException($"No account '{id}'.");
}
```

Test:

```csharp
[Fact]
public async Task GetRequiredAsync_MissingId_ThrowsAsync()
{
    var store = new InMemoryAccountStore();

    await Assert.ThrowsAsync<KeyNotFoundException>(
        () => store.GetRequiredAsync("nope"));
}
```

**The critical detail:** you must `await Assert.ThrowsAsync(...)`, and the lambda returns the `Task` (no `await` inside the lambda — hand the task to `ThrowsAsync`). If you mistakenly use the **sync** `Assert.Throws<T>(() => store.GetRequiredAsync("nope"))`, it captures the *creation* of the task, not its faulted completion — the exception happens later, on the continuation, and the assertion passes for the wrong reason (or fails confusingly). Sync assert + async method = silent wrongness; that's the thing to internalize.

### Step 5 — Async setup/teardown with `IAsyncLifetime`  `[ ]`

Phase 2 gave you constructor-as-setup and `Dispose`-as-teardown; Phase 4 gave you shared fixtures. But a constructor can't `await` — so async setup (seed a DB, open a connection, `await SaveAsync(...)`) needs `IAsyncLifetime`.

Create `tests/Ledger.Tests/Fixtures/AsyncSeededStoreFixture.cs`:

```csharp
namespace Ledger.Tests.Fixtures;

public sealed class AsyncSeededStoreFixture : IAsyncLifetime
{
    public Guid InstanceId { get; } = Guid.NewGuid();
    public InMemoryAccountStore Store { get; } = new();
    public Account Checking { get; private set; } = null!;

    // v2 signature: returns Task (v3 would be ValueTask). Async setup the ctor can't do.
    public async Task InitializeAsync()
    {
        Checking = new Account("acc-async-1", new Money(500M, new CurrencyCode("USD")));
        await Store.SaveAsync(Checking);
    }

    public Task DisposeAsync() => Task.CompletedTask; // async teardown would go here
}
```

Consume it via `IClassFixture<T>` (shared once per class), in a new `AsyncStoreQueryTests`:

```csharp
public class AsyncStoreQueryTests : IClassFixture<AsyncSeededStoreFixture>
{
    private readonly AsyncSeededStoreFixture _fixture;
    public AsyncStoreQueryTests(AsyncSeededStoreFixture fixture) => _fixture = fixture;

    [Fact]
    [Trait("Category", "Smoke")]
    public void SmokeTest() => Assert.Equal(4, 2 + 2);

    [Fact]
    public async Task SeededAccount_IsRetrievable()
    {
        var fetched = await _fixture.Store.GetAsync("acc-async-1");
        Assert.Equal(_fixture.Checking, fetched);
    }
}
```

**Verify the lifecycle** (same spirit as Phase 4's breakpoint check): set a breakpoint in `InitializeAsync`, run the class, confirm it fires **once** before the tests despite multiple tests using the fixture. The full per-test order is worth holding in your head:

```
(fixture) InitializeAsync  →  [per test] constructor → test body → Dispose  →  (fixture) DisposeAsync
```

i.e. `IAsyncLifetime.InitializeAsync` runs once (for a class/collection fixture) before any test; the per-test constructor/`Dispose` still run per test; `DisposeAsync` runs once at the end. (A *test class* can itself implement `IAsyncLifetime` for per-test async setup — then `InitializeAsync`/`DisposeAsync` wrap each test, after the constructor and before `Dispose`.)

### Step 6 — (Stretch) async `[Theory]`  `[ ]`

Async and data-driven compose cleanly — the signature is just `async Task` with theory params:

```csharp
[Theory]
[InlineData("acc-1", 100)]
[InlineData("acc-2", 250)]
public async Task SaveThenGet_VariousAccounts_RoundTrips(string id, decimal amount)
{
    var store = new InMemoryAccountStore();
    var account = new Account(id, new Money(amount, new CurrencyCode("USD")));

    await store.SaveAsync(account);

    Assert.Equal(account, await store.GetAsync(id));
}
```

All the Phase 3 serialization rules still apply to the data (`string`/`decimal` here are natively serializable — no collapse). This is just confirmation that async doesn't change the theory story.

### Step 7 — NUnit↔xUnit / v3↔v2 reflection  `[ ]`

In **Notes & questions**, capture:

- Whether `IAsyncLifetime` feels cleaner or clunkier than NUnit's async `[SetUp]`/`[TearDown]`.
- Your reaction to v2 *running* `async void` tests — does "it works but don't do it" sit comfortably, or do you prefer v3's hard fail?
- Whether the `Task`-vs-`ValueTask` `IAsyncLifetime` signature change is a real migration cost in your codebases or a trivial find/replace.
- Any place the `await Assert.ThrowsAsync` vs sync `Assert.Throws` distinction has bitten you before (it's a classic silent bug).

---

## Stretch (optional)

- **Make the seam genuinely slow:** swap `Task.Yield()` for `await Task.Delay(50)` in one method and watch test duration — a concrete feel for what real async I/O costs, and a candidate for a `[Trait("Category","Slow")]` tag (filterable per Phase 5).
- **`IAsyncLifetime` on a *collection* fixture:** promote `AsyncSeededStoreFixture` to an `ICollectionFixture<T>` shared across classes (Phase 4 pattern) and confirm `InitializeAsync` still fires exactly once for the whole collection.
- **Cancellation:** add a `CancellationToken` parameter to the store methods and a test that cancels — a realistic async concern. (xUnit v3 surfaces a per-test `TestContext.Current.CancellationToken`; v2 has no built-in equivalent — another `v3 ↔ v2` note.)
- **Deadlock demo:** write a method that does `.Result`/`.Wait()` on a `Task` inside a captured sync context and observe the classic deadlock — then fix with `await`. The "why async void / sync-over-async is dangerous" lesson, made visceral.

---

## Notes & questions

_Fill in as you go._

-
