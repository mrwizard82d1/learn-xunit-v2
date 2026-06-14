# Phases 1-4 — Port from v3 (combined)

> Summary in [`../TUTORIAL_PLAN.md`](../TUTORIAL_PLAN.md). This is the combined walkthrough for porting the v3 tutorial's Phases 1-4 work to v2.

## How to use this file

- Step headers end with `` `[ ]` ``. Flip to `` `[x]` `` when complete.
- First unchecked step = your resume point.
- "Notes & questions" at the bottom is yours.
- **Test runs:** `Ledger.Tests` Run configuration (Rider) or `dotnet test` (CLI).

## Why one combined doc instead of four

You've already done Phases 1-4 in the v3 tutorial. The xUnit-vs-NUnit translation work — `[Fact]` vs `[Test]`, per-test lifecycle, theories, fixtures — is the same lesson in v2. Rewriting four full phase docs that say "do exactly what you did before, just in this project" would be busywork.

This doc walks the *differences* explicitly and asks you to port (or rewrite) the actual test files. Where v2 behaves identically to v3, you just copy. Where v2 is different, the difference is called out with the alternative pattern.

The exception is Phase 3's serialization story, which is genuinely different in v2 and deserves real attention.

## Decisions made

- (inherits Phase 0's package decisions and the v3 tutorial's domain decisions)
- *(add others as we go)*

---

## Phase 1 — `[Fact]` and the assertion library

### What's different

Nothing meaningful. xUnit v2.9.3's `[Fact]`, `Assert.Equal`, `Assert.Throws<T>`, `Assert.IsType`, etc. all have the same signatures and semantics as v3. The argument-order convention (`Assert.Equal(expected, actual)`) is identical.

### Step 1.1 — Port the Money tests  `[x]`

Copy these test files from `~/professional/projects/learn-xunit/tests/Ledger.Tests/` to `~/professional/projects/learn-xunit-v2/tests/Ledger.Tests/`:

- `MoneyConstructorTests.cs`
- `MoneyEqualityTests.cs`
- `MoneyAddTests.cs`
- `MoneySubtractTests.cs`
- `MoneyArithmeticInvariantsTests.cs`
- `MoneyNegateTests.cs`

The contents need **no edits** for v2. Run `dotnet test`; should be green.

### Step 1.2 — Port `CurrencyCodeTests`  `[x]`

Copy `CurrencyCodeTests.cs` over. No edits needed. Run; green.

---

## Phase 2 — Lifecycle (constructor, `IDisposable`, `IAsyncLifetime`)

### What's different

Nothing meaningful. xUnit v2 has the same per-test instance lifecycle, the same constructor-as-setup pattern, `IDisposable.Dispose` as teardown, and `IAsyncLifetime` for async setup/teardown.

### Step 2.1 — Port `AccountRepositoryTests`  `[x]`

Copy `AccountRepositoryTests.cs` over. No edits needed. Run; green.

---

## Phase 3 — `[Theory]`, `[InlineData]`, `[MemberData]`, `[ClassData]`

### What's different — and what's the same

**Same in v2**: `[Theory]`, `[InlineData]`, `[MemberData]`, `[ClassData]`, `TheoryData<T1, T2, ...>` — all present, same signatures. The argument constraints on `[InlineData]` (no `decimal` literals, no `new SomeClass(...)`, etc.) are identical.

**Different in v2**: the **theory data serialization story**.

Recall the v3 picture from your prior work:

- xUnit's runner needs to serialize each `[Theory]` case's arguments to give the case a stable identity and per-case display name.
- `Money` (a record with primitive-typed fields) isn't auto-serialized — xUnit treats it as opaque.
- In v3, the fix was `Xunit.Sdk.IXunitSerializer<Money>` registered via assembly attribute — *external* to `Money`, keeps the production type framework-agnostic.

**v2 doesn't have `IXunitSerializer`** (the external-class pattern is v3-only). v2's options are narrower:

1. **`IXunitSerializable` on the type itself.** Implement `Xunit.Abstractions.IXunitSerializable` on `Money` (or `CurrencyCode`). Means production code takes a test-framework dependency. Awkward for records (positional constructors don't play nicely with the parameterless-constructor requirement).
2. **Decompose to primitives in `TheoryData`** (the Step 4 form from the v3 tutorial). `TheoryData<decimal, decimal, decimal, string>`, construct `Money` inside the test method. Same trade-off as in v3 — typing-vs-intent.
3. **Accept the discovery-time collapse.** Non-serializable theory data still *executes* fine — all rows run, all results report. What you lose is **discovery-time identity**: on the CLI/VSTest path xUnit emits `Non-serializable data … found …; falling back to single test case`, so the theory is discovered as **one** addressable case instead of N. You can't filter or re-run a single row, and Test Explorer groups them. (Note: this collapse is **runner-dependent** — see Step 3.1. Rider/ReSharper enumerates rows on its own discovery path and is unaffected.)

The right answer for this port: **Option 2 (decompose to primitives)**. Same shape as v3's Step 4, just without the Step 5 "and then add the external serializer" payoff that doesn't exist in v2.

### Step 3.1 — Observe the collapse (discovery vs execution, and the runner split)  `[x]`

> **Correction from the original plan (2026-06-14).** This step was written assuming the ported `MoneyAddTests.cs` would carry `TheoryData<Money, Money, Money>` and that you'd watch it "collapse to `[3 cases]`." Two things turned out differently, verified empirically:
> 1. The v3 file arrived **already decomposed** for `Money` — `AddCases` was `TheoryData<decimal, decimal, decimal, CurrencyCode>`. The surviving non-serializable argument was **`CurrencyCode`** (a `record`, not xUnit-serializable), not `Money`.
> 2. The collapse is **not** a visual "`[3 cases]`" and **not** silent-by-default. What actually happens is below.

**Make the collapse visible.** By default it's invisible — turn diagnostics on in `tests/Ledger.Tests/xunit.runner.json`:

```json
{
  "$schema": "https://xunit.net/schema/current/xunit.runner.schema.json",
  "showLiveOutput": true,
  "diagnosticMessages": true
}
```

**Observe** (with `CurrencyCode` still in the `TheoryData`):

```
dotnet test --filter "FullyQualifiedName~MoneyAdd"
```

You'll see two numbers that look contradictory until you separate the phases:

- `Discovered: Ledger.Tests (61 test cases to be run)` — the **discovery** count. The full suite is 63; the theory collapsed from 3 rows to **1** addressable case (−2 = 61). Alongside it, the warning fires:
  `Non-serializable data ('System.Object[]') found for '…MoneyAddTests.TwoMonies_Add_ProducesExpectedSum'; falling back to single test case.`
- `total: 8` — the **execution** count. When the single collapsed case runs, xUnit fans it back out and runs all three rows, reporting 3 results (5 facts + 3 rows = 8).

So **non-serializable data never stops rows from running** — you still get every result. What collapses is **discovery-time identity**: one case you can address/filter/re-run instead of three.

**The runner split (important).** Open Rider's Test Explorer on the same code: it shows the 3 rows individually and emits *no* warning. That's because Rider/ReSharper enumerates theory rows on its **own** discovery path, which doesn't go through xUnit's serialization requirement. Only the CLI/VSTest path (`dotnet test`) collapses. So "it looks fine in Rider" is not evidence the data is serializable — always check the CLI.

**`xUnit ↔ NUnit` note.** You never hit this in NUnit because NUnit identifies `[TestCase]`/`[TestCaseSource]` cases by source + index in-process; it doesn't round-trip the arguments through serialization. xUnit deliberately wants theory data serializable so each case has a stable identity that survives across processes — which is what powers reliable single-case re-run, the test explorer, and parallelization. Not something you missed in NUnit; a genuine framework-design difference.

### Step 3.2 — Make the data serializable (`CurrencyCode` → `string`)  `[x]`

The fix is to push the last non-serializable argument out of the theory data: carry the currency as a `string` (natively serializable) and construct `CurrencyCode` inside the test body.

```csharp
public static TheoryData<decimal, decimal, decimal, string> AddCases =>
    new()
    {
        { 607.37M, 733.74M, 1341.11M, "DKK" },
        { 871.13M, -892.52M, -21.39M, "BSD" },
        { 0M, 913.38M, 913.38M, "IQD" },
    };

[Theory]
[MemberData(nameof(AddCases))]
public void TwoMonies_Add_ProducesExpectedSum(decimal oneAmount, decimal anotherAmount, decimal expectedSum, string currency)
{
    var actual = new Money(oneAmount, new CurrencyCode(currency))
        .Add(new Money(anotherAmount, new CurrencyCode(currency)));
    Assert.Equal(new Money(expectedSum, new CurrencyCode(currency)), actual);
}
```

Re-run `dotnet test --filter "FullyQualifiedName~MoneyAdd"`. Verified after-state:

- Discovery flips to `63 test cases` — the theory now enumerates to 3 (serializable data → stable per-case identity).
- The non-serializable **warning disappears**.
- `total:` stays **8** — execution is unchanged; same rows, same assertions. (Proof the collapse was only ever about *discovery*, never about whether rows ran.)
- Rider is unchanged — it already showed 3 rows; it had nothing to react to.

The payoff is the restored discovery identity: you can now filter or re-run a single row from the CLI, and the test explorer addresses each case independently.

**On leaving `diagnosticMessages: true`:** worth keeping on during dev. The collapse is silent by default, so the diagnostic is your only signal that a theory quietly dropped to a single case — a real, otherwise-invisible bug class.

### Step 3.3 — Delete `MoneyXunitSerializer` if you ported it  `[x]`

If you copied `tests/Ledger.Tests/Infrastructure/MoneyXunitSerializer.cs` over, delete it. The class implements `Xunit.Sdk.IXunitSerializer` which doesn't exist in v2. Similarly, delete the `[assembly: RegisterXunitSerializer(...)]` line (also v3-only).

Also delete `MoneyXunitSerializerTests.cs` and the empty folders.

This is one of the very few places where the v3 work doesn't carry forward — the external-serializer pattern was a v3-only luxury.

### Step 3.4 — Decision: implement `IXunitSerializable` on `Money` or `CurrencyCode`?  `[x]`

You can stop at Step 3.2 — the decomposed-to-primitives form works fine and is good practice. The `TheoryData<Money, Money, Money>` form *isn't actually needed*; the test data is just as expressive with primitives, and the test body reconstruction isn't onerous.

But if you want the v2 equivalent of "Money flows directly through theory data with per-case display," the path is:

- Implement `Xunit.Abstractions.IXunitSerializable` on `Money` (or `CurrencyCode`, since `Money` contains it).
- Adds a test-framework dependency to production code — which is the thing the v3 external-serializer pattern was designed to avoid.

I'd skip this. The decomposed form is more honest about what's actually flowing through the test data. If you do want to try `IXunitSerializable` for the experience, it's a small exercise but adds production-code coupling you'll want to remove later.

### Step 3.5 — `[ClassData]` and Money construction  `[x]`

The v3 tutorial's Step 4 also showed a `[ClassData]` form using `TheoryData<Money, Money, Money>`. Same v2 serialization issue applies. If you ported any `[ClassData]` examples, refactor them to the decomposed `TheoryData<decimal, decimal, decimal, string>` shape.

---

## Phase 4 — `IClassFixture<T>` and `ICollectionFixture<T>`

### What's different

Nothing meaningful. `IClassFixture<T>`, `ICollectionFixture<T>`, `[CollectionDefinition]`, `[Collection("...")]` — all present in v2, same signatures, same semantics.

The Phase 4 work from v3 ports unchanged: `SeededAccountsFixture`, the marker class, both consuming test classes.

### Step 4.1 — Port the fixture work  `[ ]`

> **Layout correction (2026-06-14, verified against the v3 project).** This is **two files**, not four. In v3, `Fixtures/SeededAccountsFixture.cs` bundles *three* types: the `SeededAccountsFixture` itself, the `SeededAccountsCollection` marker (`[CollectionDefinition("Seeded accounts")]` + `ICollectionFixture<SeededAccountsFixture>`), **and** `AccountInventoryTests` (the second consumer). `AccountInventoryTests` is **not** its own file. So copying these two files brings everything:

```bash
cp ~/professional/projects/learn-xunit/tests/Ledger.Tests/Fixtures/SeededAccountsFixture.cs tests/Ledger.Tests/Fixtures/
cp ~/professional/projects/learn-xunit/tests/Ledger.Tests/AccountQueryTests.cs tests/Ledger.Tests/
```

(Create `tests/Ledger.Tests/Fixtures/` first if it doesn't exist.) No edits needed — `IClassFixture<T>`, `ICollectionFixture<T>`, `[CollectionDefinition]`, `[Collection]` are identical in v2. Run `dotnet test`; all green, discovery count climbs by the new cases.

Note: `AccountScenarioTests.cs` is **not** part of this step — it uses `ITestOutputHelper` + `[Trait]`, which is Phase 5 material. Leave it in v3 for now.

### Step 4.2 — Validate via the breakpoint check  `[ ]`

Same exercise as v3 Phase 4 Step 5: set a breakpoint in `SeededAccountsFixture`'s constructor, run the full suite, observe the breakpoint hits **once** despite multiple tests using the fixture. Confirms the lifecycle is the same.

---

## Net result of the port

You should now have:

- All v3 production code present and tested in v2.
- All v3 test files ported, with the Phase 3 serialization adjustment (decompose to primitives in `TheoryData`).
- No `IXunitSerializer`-shaped infrastructure (it doesn't exist in v2).
- Same `Lifecycle_PartOne/PartTwo` documentation tests in `AccountQueryTests`.
- Same `[Collection("Seeded accounts")]` cross-class sharing.

What you haven't done yet:

- Phase 5 (Output, traits, skipping) — needs its own v2 doc because the substantive differences (live output works, VSTest filters, `Xunit.SkippableFact`) deserve real attention.
- Phases 6-10 — write fresh in v2 idiom as you reach them.

---

## Stretch (optional)

- **Run the v2 suite and the v3 suite side-by-side** (both still on disk). Compare runner output formats, test discovery times, IDE integration. Build the personal "what works where" mental map empirically.
- **Try implementing `IXunitSerializable` on `Money`** as a one-time experiment to feel the v2 mechanic, then revert. Useful for recognizing the pattern in older codebases.

---

## Notes & questions

_Fill in as you go._

-
