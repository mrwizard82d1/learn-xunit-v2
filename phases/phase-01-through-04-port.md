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

### Step 2.1 — Port `AccountRepositoryTests`  `[ ]`

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
3. **Accept the per-case display collapse.** `TheoryData<Money, Money, Money>` works fine for test *execution*; you just lose the per-case display in the runner output. Tests still run, just identified by number (`[3 cases]`) instead of by parameter values.

The right answer for this port: **Option 2 (decompose to primitives)**. Same shape as v3's Step 4, just without the Step 5 "and then add the external serializer" payoff that doesn't exist in v2.

### Step 3.1 — Smoke-test the port  `[ ]`

Run the existing Money tests you ported in Phase 1. If you copied `MoneyAddTests.cs` from v3 unchanged, it'll currently have `TheoryData<Money, Money, Money>` — which works for execution but causes per-case display collapse in v2. No code changes needed yet; just observe the runner output:

```
dotnet test --filter "FullyQualifiedName~MoneyAdd"
```

Note whether `Add_ProducesExpectedSum` displays cases individually or collapses to `[3 cases]`. (In v2, it should collapse — no auto-serialization of `Money`.)

### Step 3.2 — Decompose to primitives  `[ ]`

Refactor `MoneyAddTests.AddCases` from `TheoryData<Money, Money, Money>` to `TheoryData<decimal, decimal, decimal, string>` and construct `Money` instances inside the test body. The shape from the v3 tutorial's Step 4:

```csharp
public static TheoryData<decimal, decimal, decimal, string> AddCases =>
    new()
    {
        { 607.37M, 733.74M, 1341.11M, "DKK" },
        { 871.13M, -892.52M, -21.39M, "JPY" },
        { 0M, 913.38M, 913.38M, "BWP" },
    };

[Theory]
[MemberData(nameof(AddCases))]
public void Add_ProducesExpectedSum(decimal a, decimal b, decimal expectedSum, string currency)
{
    var actual = new Money(a, new CurrencyCode(currency))
        .Add(new Money(b, new CurrencyCode(currency)));
    Assert.Equal(new Money(expectedSum, new CurrencyCode(currency)), actual);
}
```

Run again. The per-case display should now be back — each case shows with its parameter values in the runner output.

### Step 3.3 — Delete `MoneyXunitSerializer` if you ported it  `[ ]`

If you copied `tests/Ledger.Tests/Infrastructure/MoneyXunitSerializer.cs` over, delete it. The class implements `Xunit.Sdk.IXunitSerializer` which doesn't exist in v2. Similarly, delete the `[assembly: RegisterXunitSerializer(...)]` line (also v3-only).

Also delete `MoneyXunitSerializerTests.cs` and the empty folders.

This is one of the very few places where the v3 work doesn't carry forward — the external-serializer pattern was a v3-only luxury.

### Step 3.4 — Decision: implement `IXunitSerializable` on `Money` or `CurrencyCode`?  `[ ]`

You can stop at Step 3.2 — the decomposed-to-primitives form works fine and is good practice. The `TheoryData<Money, Money, Money>` form *isn't actually needed*; the test data is just as expressive with primitives, and the test body reconstruction isn't onerous.

But if you want the v2 equivalent of "Money flows directly through theory data with per-case display," the path is:

- Implement `Xunit.Abstractions.IXunitSerializable` on `Money` (or `CurrencyCode`, since `Money` contains it).
- Adds a test-framework dependency to production code — which is the thing the v3 external-serializer pattern was designed to avoid.

I'd skip this. The decomposed form is more honest about what's actually flowing through the test data. If you do want to try `IXunitSerializable` for the experience, it's a small exercise but adds production-code coupling you'll want to remove later.

### Step 3.5 — `[ClassData]` and Money construction  `[ ]`

The v3 tutorial's Step 4 also showed a `[ClassData]` form using `TheoryData<Money, Money, Money>`. Same v2 serialization issue applies. If you ported any `[ClassData]` examples, refactor them to the decomposed `TheoryData<decimal, decimal, decimal, string>` shape.

---

## Phase 4 — `IClassFixture<T>` and `ICollectionFixture<T>`

### What's different

Nothing meaningful. `IClassFixture<T>`, `ICollectionFixture<T>`, `[CollectionDefinition]`, `[Collection("...")]` — all present in v2, same signatures, same semantics.

The Phase 4 work from v3 ports unchanged: `SeededAccountsFixture`, the marker class, both consuming test classes.

### Step 4.1 — Port the fixture work  `[ ]`

Copy these from v3 over to v2:

- `tests/Ledger.Tests/Fixtures/SeededAccountsFixture.cs` (or wherever you ended up placing it after the Phase 4 extraction)
- The `SeededAccountsCollection` marker class
- `AccountQueryTests.cs`
- `AccountInventoryTests.cs`

Run; all green.

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
