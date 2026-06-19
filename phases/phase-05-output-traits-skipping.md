# Phase 5 — Output, traits, skipping (v2)

> Summary in [`../TUTORIAL_PLAN.md`](../TUTORIAL_PLAN.md). This is the detailed walkthrough. First *fresh* v2 phase (Phases 1–4 were a port); this is also where v2+VSTest's tooling **beats** the v3+MTP experience, so expect `v3 ↔ v2` call-outs.

## How to use this file

- Step headers end with `` `[ ]` ``. Flip to `` `[x]` `` when complete (you do this at your commit step).
- First unchecked step = your resume point.
- "Notes & questions" at the bottom is yours.
- **Test runs:** `Ledger.Tests` Run configuration (Rider) or `dotnet test` (CLI).

## Goal

Three practical knobs that come up in real test maintenance, not in tutorials:

- **`ITestOutputHelper`** for diagnostic output that survives the runner's silence-by-default — and, in v2+VSTest, output you can actually *see live on passing runs* (the thing v3+MTP couldn't do).
- **`[Trait]`** for tagging tests by category/slowness/owner — filterable from the CLI with the *standard VSTest grammar* (`dotnet test --filter`), exactly as every mainstream .NET tutorial documents.
- **Skipping** — `[Fact(Skip = "…")]` (unconditional, built-in) and **conditional** skip, where v2 has no native `Assert.Skip*` and the answer is the `Xunit.SkippableFact` package.

Each is small in isolation. Together they're what makes a test suite *operable* at scale. This phase mostly demonstrates patterns rather than driving design — the rhythm is "wire it up, see it work."

## Decisions made

- (inherits Phase 0 package decisions + the 1–4 port patterns: smoke-as-canary, `CurrencyCode` for currency, fixture-as-immutable-seed.)
- **Conditional skip via `Xunit.SkippableFact`** (decided here). v2 has no native runtime skip; the community-standard package fills the gap. Version recorded in Step 6 once it lands.
- **Smoke method naming standardized to singular `SmokeTest`** (decided 2026-06-16, before Step 4 tagging). Each smoke is a single `[Fact]`, so the plural `SmokeTests` was a misnomer; the 6 plural ones (the `Money*` files) were renamed. The standalone `SmokeTest.cs` keeps its `ArithmeticSmoke` method — it's the harness-level canary, and a method can't share its enclosing type's name (CS0542) without also renaming the class. **Reserved option ("future draft pick"):** rename that class for full uniformity later if desired.
- **`showLiveOutput: true` works in v2+VSTest — verified 2026-06-15 (the Phase 0 bet paid off).** Live `[OUTPUT]` lines stream during execution on **passing** tests at *default* verbosity — plain `dotnet test`, no flags. xUnit also prints a captured `Output:` block after `[PASS]`. `--logger "console;verbosity=detailed"` is accepted and adds VSTest's own `Standard Output Messages:` block (and doubles the `[xUnit.net]` diagnostic lines — cosmetic). So live output is available three ways (plain run, `--logger`, failure path); in v3+MTP none worked for passing tests. (Prediction-beat: I'd expected only detailed verbosity to surface it; plain run does.)
- *(add others as we go)*

## `v3 ↔ v2` orientation (why this phase feels different)

In the v3 tutorial, Phase 5 was a story of *broken tooling*: `--show-live-output` rejected by the MTP parser, `--logger`/`--filter` non-existent (MTP wanted `-- --filter-trait`), `showLiveOutput` dead in the MTP pipeline. You worked around all of it. **None of that applies here.** v2 runs on VSTest via `xunit.runner.visualstudio`, so the CLI knobs in every mainstream tutorial work as written. The flip side: conditional skip is *worse* in v2 — v3 has native `Assert.Skip*`; v2 needs a package. So this phase is mostly "the tools work now," with one "v2 is the one that needs a crutch" exception.

---

## Steps

### Step 1 — Port `AccountScenarioTests` (smoke + `ITestOutputHelper` scenario)  `[x]`

The v3 project already has the class this phase is built around. Port it:

```bash
cp ~/professional/projects/learn-xunit/tests/Ledger.Tests/AccountScenarioTests.cs tests/Ledger.Tests/
dotnet test
```

No edits needed. Expect green, count **70 → 72**. The file brings three things you'll use across this phase:

- A `SmokeTest()` already tagged `[Trait("Category", "Smoke")]` (Step 3's filtering uses it).
- `ITestOutputHelper` injected via constructor — same DI-via-constructor pattern as fixtures. xUnit supplies it automatically.
- `OpenSeveralAccounts_QueryEach_AllPresent`, which writes three `_output.WriteLine(...)` lines (Step 2's output demonstration).

**`v3 ↔ v2` gotcha (verified the hard way):** in xUnit **v2**, `ITestOutputHelper` lives in the **`Xunit.Abstractions`** namespace (assembly `xunit.abstractions.dll`), *not* `Xunit`. In v3 it was moved into `Xunit`, so the ported file compiled there on the global `using Xunit` alone — but in v2 that global using doesn't reach it, and the build fails with `'ITestOutputHelper' could not be found`. Fix: add a second global using to `Ledger.Tests.csproj` so every test file is covered:

```xml
<ItemGroup>
  <Using Include="Xunit" />
  <Using Include="Xunit.Abstractions" />
</ItemGroup>
```

(Or a per-file `using Xunit.Abstractions;`.) This namespace move is one of the most common surprises when reading v3 docs/blog posts against a v2 codebase.

**NUnit ↔ xUnit:** `Console.WriteLine` in NUnit gets collected and printed under per-test sections. In xUnit it's *captured but invisible* — xUnit refuses to surface raw console output because parallel tests would interleave. `ITestOutputHelper.WriteLine` is the xUnit-correct way to log; the runner knows which test each line belongs to. The API is deliberately tiny: `WriteLine(string)` + a format overload, no `Write` (every call is a whole line).

### Step 2 — See live output actually work (the Phase 0 bet pays off)  `[x]`

Back in Phase 0 you set `showLiveOutput: true` in `xunit.runner.json` on the bet that v2+VSTest delivers what v3+MTP couldn't. The smoke test wrote nothing, so you never saw it. `OpenSeveralAccounts_QueryEach_AllPresent` writes three lines — this is where you collect.

> **Honest framing (learned from Phase 3):** I'm presenting this as something to *verify*, not a guarantee. `dotnet test`'s console logger can be picky about surfacing passing-test output. Run the variants below and record which one(s) actually show the `_output` lines. Whatever you observe is the truth we'll write into the Decisions section — don't trust my prediction over your terminal.

Three things to try (your `xunit.runner.json` already has `showLiveOutput: true` and `diagnosticMessages: true`):

```bash
# 1. Plain run — does live output surface for a passing test?
dotnet test --filter "FullyQualifiedName~OpenSeveralAccounts"

# 2. Detailed console verbosity — VSTest grammar that works here (didn't exist in v3+MTP)
dotnet test --filter "FullyQualifiedName~OpenSeveralAccounts" --logger "console;verbosity=detailed"
```

Look for the `Opened checking: …` / `Opened savings: …` / `Both created accounts verified present.` lines. Expectation (to confirm or refute): at least the detailed-verbosity run surfaces them, and `--logger` itself is accepted — the two things that flat-out failed in v3+MTP.

**Runner-agnostic baseline (always works):** temporarily flip an assertion in that test to fail (e.g. `Assert.False(repo.Contains(checking.Id))`), run, and confirm the `_output` lines print in the failure block — then revert. xUnit *unconditionally* surfaces captured output on failure; that path is reliable regardless of runner. The interesting v2 question is whether it also surfaces *live on a pass*.

Record in **Notes & questions**: which command surfaced live output (if any), and whether Rider's runner shows it inline (it typically does — another runner-path difference, same theme as Phase 3).

**`v3 ↔ v2` call-out:** in v3 you logged a long list of dead flags (`--show-live-output on`, `-show-output-live`, `showLiveOutput` in json — all broken via MTP, [xunit#3468](https://github.com/xunit/xunit/issues/3468)) and concluded the only reliable surface was the failure path. Here, `--logger` and `console;verbosity=detailed` are standard VSTest and should just work.

### Step 3 — `[Trait]` and CLI filtering (standard VSTest grammar)  `[x]`

`[Trait]` is a key-value tag on any test method or class; xUnit assigns no meaning to the names — convention is yours. The ported `AccountScenarioTests.SmokeTest` already carries `[Trait("Category", "Smoke")]`, so you can filter immediately:

```bash
dotnet test --filter "Category=Smoke"
```

You should see exactly **one** test run — the tagged smoke. Everything else (including the other, not-yet-tagged smokes) is excluded.

**VSTest `--filter` grammar** (this is the v2 advantage — the syntax every .NET tutorial assumes):

- `--filter "Category=Smoke"` — trait equals (trait *name* is the property).
- `--filter "Category!=Slow"` — exclusion.
- `--filter "FullyQualifiedName~MoneyAdd"` — substring match (you used this in Phase 3).
- `--filter "Category=Smoke|Category=Integration"` — boolean OR; `&` for AND.

**`v3 ↔ v2` call-out:** v3+MTP rejected `--filter` entirely and made you write `-- --filter-trait "Category=Smoke"` with a different per-axis flag family (`--filter-class`, `--filter-method`, `--filter-query`, …). v2's single-string expression grammar is the mainstream one — what you'll find in docs, blog posts, and CI examples.

**NUnit ↔ xUnit:** `[Category("Integration")]` → `[Trait("Category", "Integration")]`. NUnit's `[Category]` is single-valued; `[Trait]` is key-value, so one test can be tagged on multiple orthogonal axes at once (`("Category","Integration")` *and* `("Owner","billing")` *and* `("Speed","slow")`).

### Step 4 — Tag every smoke and build a canary filter (refactor)  `[x]`

Add `[Trait("Category", "Smoke")]` to every `SmokeTest()` across the suite. Classes with a smoke: `AccountQueryTests`, `AccountInventoryTests` (inside `Fixtures/SeededAccountsFixture.cs`), `AccountRepositoryTests`, `AccountScenarioTests` (already tagged), `CurrencyCodeTests`, `MoneyAddTests` (its `SmokeTests`), `MoneyConstructorTests`, `MoneyEqualityTests`, `MoneyNegateTests`, `MoneySubtractTests`, `MoneyArithmeticInvariantsTests`, and the standalone `SmokeTest` class. (Rider's Structural Search/Replace can do this in one pass, or walk them manually.)

Then:

```bash
dotnet test --filter "Category=Smoke"
```

…runs the whole canary set. That's useful as a fast pre-commit "is everything basically wired up?" check, a CI smoke stage that fails fast before the full suite, and a quick health check during cross-file refactors.

Other axes worth knowing (don't add yet): `Category=Slow` for >1s tests (`--filter "Category!=Slow"` for fast local runs), `Category=Integration` (Phase 9), `Owner=…` for larger teams. Trait *names* are conventional, not enforced — pick what gives useful filters; don't over-engineer.

### Step 5 — Unconditional skip with `[Fact(Skip = "…")]`  `[x]`

Add to `AccountScenarioTests`:

```csharp
[Fact(Skip = "Demonstration — not a real test")]
public void IntentionallySkipped_DemonstratesSkipBehavior()
{
    Assert.True(false); // never executes
}
```

Run `dotnet test`. You should see one test reported as **Skipped** with the reason; the body never runs (`Assert.True(false)` doesn't fire).

**This is honest behavior** — a skipped test is a distinct outcome, not a silent pass. CI counts skips separately; reviewers see the reason. Compare: commenting out a test (silent disappearance) or deleting it (intent lost). `[Fact(Skip)]` keeps the test visibly skipped with a reason. Skip when you can't fix it yet but don't want to lose it; delete when it's no longer relevant.

**NUnit ↔ xUnit:** `[Ignore("reason")]` → `[Fact(Skip = "reason")]`. Same semantics, same honesty-as-default. Identical in v2 and v3.

### Step 6 — Conditional (runtime) skip with `Xunit.SkippableFact`  `[ ]`

This is the genuine v2 gap. Sometimes you skip *at runtime* based on a condition (OS, env var, network, optional dependency). **v2 has no native `Assert.Skip*`** — that's a v3-only addition. The v2 answer is the [`Xunit.SkippableFact`](https://github.com/AArnott/Xunit.SkippableFact) package.

Add it (record the landed version in Decisions):

```bash
dotnet add tests/Ledger.Tests package Xunit.SkippableFact --version 1.5.61
```

Then use `[SkippableFact]` (not `[Fact]`) and the `Skip` helper:

```csharp
[SkippableFact]
public void EnvironmentSpecific_SkipsOutsideCi()
{
    Skip.IfNot(
        Environment.GetEnvironmentVariable("CI") == "true",
        "This test only runs in CI (where the environment is reproducible).");

    // ... real body that depends on the CI environment ...
    Assert.True(true);
}
```

`Skip` API: `Skip.If(condition, "reason")` skips when true; `Skip.IfNot(condition, "reason")` skips when false (the common "skip unless prerequisite met" form). `[SkippableTheory]` exists for theories. `SkippableFactAttribute` is in the `Xunit` namespace (global using covers it).

**Critical gotcha:** the `[SkippableFact]` attribute is mandatory. Under a plain `[Fact]`, the `SkipException` that `Skip.*` throws isn't recognized as a skip — it surfaces as a **failure**. The attribute's discoverer is what translates the exception into a Skipped result.

Verify both branches:

```bash
dotnet test --filter "FullyQualifiedName~EnvironmentSpecific"
# Skipped: "This test only runs in CI..."

CI=true dotnet test --filter "FullyQualifiedName~EnvironmentSpecific"
# Runs (and passes)
```

**`v3 ↔ v2` call-out:** in v3 this is native — `Assert.Skip("…")`, `Assert.SkipWhen(cond, "…")`, `Assert.SkipUnless(cond, "…")`, no package. The mapping is `Skip.IfNot` ⇄ `Assert.SkipUnless`, `Skip.If` ⇄ `Assert.SkipWhen`. If you migrate a v2 suite to v3 later, you delete the `Xunit.SkippableFact` package, swap `[SkippableFact]`→`[Fact]`, and replace `Skip.*`→`Assert.Skip*`.

**NUnit ↔ xUnit:** NUnit's `Assert.Ignore("reason")` is the closest equivalent — skip-at-runtime with honest reporting.

### Step 7 — NUnit↔xUnit reflection  `[ ]`

In **Notes & questions** below, capture:

- How often you reached for `Console.WriteLine` in NUnit, and whether `ITestOutputHelper` feels heavier-but-cleaner or just heavier — *and* whether seeing live output finally work (Step 2) changes that.
- Whether `[Trait]`'s key-value form buys you anything over NUnit's single-valued `[Category]`.
- Whether xUnit's "Skipped is a real outcome" philosophy matches how you treated ignored tests in NUnit, or whether you tended to delete-and-forget.
- The `Xunit.SkippableFact`-vs-native-`Assert.Skip*` split: does needing a package for conditional skip bother you enough to weigh in a v2-vs-v3 decision at work?

---

## Stretch (optional)

- **`dotnet test --filter` deep-dive:** read the [VSTest selective-test docs](https://learn.microsoft.com/en-us/dotnet/core/testing/selective-unit-tests). Learn the full operator set (`=`, `!=`, `~`, `|`, `&`) and properties (`FullyQualifiedName`, `Name`, trait names). This is the grammar your CI will use.
- **`xunit.runner.json` knobs:** explore `methodDisplay`, `methodDisplayOptions`, `parallelizeTestCollections`, `maxParallelThreads`. Phase 8 (parallelism) returns to these.
- **Compare output between `dotnet test` and Rider** for the same failing test — confirm the CLI prints captured output on failure while Rider tends to always show it. Knowing the difference saves "why does it look different in CI?" confusion.
- **Capture and assert on output:** for a test that logs structured info, consider whether asserting on captured output is ever appropriate (usually not — prefer asserting on state). A good reflection on the line between diagnostics and assertions.

---

## Notes & questions

_Fill in as you go._

-
