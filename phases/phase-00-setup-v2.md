# Phase 0 — Setup (v2)

> Summary in [`../TUTORIAL_PLAN.md`](../TUTORIAL_PLAN.md). This is the detailed walkthrough.

## How to use this file

- Step headers end with `` `[ ]` ``. Flip to `` `[x]` `` when complete.
- First unchecked step = your resume point.
- "Notes & questions" at the bottom is yours.
- **Test runs:** `Ledger.Tests` Run configuration (Rider) or `dotnet test` (CLI).

## Goal

Stand up the v2-correct project, port the Ledger domain code from the v3 project unchanged, confirm everything wires up with a working smoke test.

## Decisions made

- **xUnit v2.9.3.** Matches your most-recent production projects (`Encapture.UIP.Apps.CIF2020.Tests` is on it). Negligible differences from 2.9.0 — same API surface, same conceptual model — so picking the latest 2.9.x is consequence-free.
- **VSTest runner via `xunit.runner.visualstudio`.** No MTP. Standard .NET test wiring; works as the broader ecosystem documents.
- **.NET 10 pinned via `global.json`.** Same as the v3 project. Reproducible across machine reinstalls.
- **Reuse the v3 tutorial's domain code unchanged.** `Money`, `CurrencyCode`, `Account`, `AccountRepository` all v2-compatible without modification; the framework boundary doesn't affect production code.
- **`OutputType=Library` (not `Exe`).** v3+MTP required `Exe`; v2+VSTest uses the standard `Library` shape. Less csproj fiddling.
- **Landed package versions** (from the `dotnet new xunit` template on SDK 10.0.109; no manual bumps needed — `xunit` already came in at the 2.9.3 target):
  - `xunit` **2.9.3**
  - `xunit.runner.visualstudio` **3.1.4**
  - `Microsoft.NET.Test.Sdk` **17.14.1**
  - `coverlet.collector` **6.0.4** (template extra; harmless, left in place)

---

## Steps

### Step 1 — Create the solution structure  `[x]`

In `~/professional/projects/learn-xunit-v2/` (already exists, with `LICENSE` and `README.md`), you want this layout:

```
learn-xunit-v2/
├── LICENSE
├── README.md
├── global.json
├── Ledger.sln
├── src/
│   └── Ledger/
│       └── Ledger.csproj
└── tests/
    └── Ledger.Tests/
        └── Ledger.Tests.csproj
```

Copy `global.json` from `~/professional/projects/learn-xunit/` — same .NET 10 SDK pin — **but delete the `test` block before saving.** The v3 file contains:

```json
"test": {
    "runner": "Microsoft.Testing.Platform"
}
```

That block opts `dotnet test` into the MTP execution path, which is exactly what v2 (VSTest via `xunit.runner.visualstudio`) does *not* use — leaving it in can make `dotnet test` fail to discover or run tests. The v2 `global.json` should contain only the `sdk` section:

```json
{
    "sdk": {
        "version": "10.0.107",
        "rollForward": "latestFeature"
    }
}
```

(`rollForward: latestFeature` means the floor is `10.0.107` but any installed `10.0.1xx` SDK ≥ that — e.g. `10.0.109` — satisfies it. Keep the floor low for portability; don't bump it to whatever you happen to have installed.)

CLI commands to run yourself when you're ready:

```
dotnet new sln -n Ledger --format sln
dotnet new classlib -n Ledger -o src/Ledger
dotnet new xunit -n Ledger.Tests -o tests/Ledger.Tests
dotnet sln add src/Ledger tests/Ledger.Tests
dotnet add tests/Ledger.Tests/Ledger.Tests.csproj reference src/Ledger/Ledger.csproj
```

Two important version-of-things-changing-on-me notes:

- **`dotnet new xunit`** (no `3`) — the v2 template. Generates an `xunit` 2.x csproj using `xunit.runner.visualstudio` + `Microsoft.NET.Test.Sdk`. Compare to `dotnet new xunit3` from the v3 project — different template, different generated file.
- **`dotnet new classlib`** — `OutputType` defaults to `Library`. Leave it. No `Exe`, no MTP toggles.
- **`--format sln`** — .NET 10's `dotnet new sln` defaults to the new `.slnx` (XML) format. Forcing classic `.sln` because Rider's `.slnx` support is still rough under Gateway/Remote Development. Drop the flag once Rider's remote `.slnx` handling is solid.

After running these, verify both csproj files have `<Nullable>enable</Nullable>` and `<ImplicitUsings>enable</ImplicitUsings>`. The standard templates usually include both.

Delete the `Class1.cs` placeholder that `dotnet new classlib` generates in `src/Ledger/`.

### Step 2 — Pin xUnit to 2.9.3  `[x]`

The `dotnet new xunit` template pins a specific version; verify and adjust to 2.9.3 if needed.

Check what the template produced:

```
grep -E '(xunit|Microsoft.NET.Test.Sdk)' tests/Ledger.Tests/Ledger.Tests.csproj
```

If `xunit` is not exactly `2.9.3`, update:

```
dotnet add tests/Ledger.Tests package xunit --version 2.9.3
```

Also worth checking — the template's `xunit.runner.visualstudio` and `Microsoft.NET.Test.Sdk` versions may or may not be current. For tutorial purposes the template defaults are fine; for matching your production projects exactly, pin to whatever combination they use. (Your scan showed a mix of `xunit.runner.visualstudio` 2.4.x through 3.1.x — any of those works against `xunit` 2.9.x.)

Record the exact versions in the **Decisions made** section above once you know what landed.

### Step 3 — Configure `xunit.runner.json` (with `showLiveOutput`)  `[x]`

Copy `xunit.runner.json` from `~/professional/projects/learn-xunit/tests/Ledger.Tests/` — same file as the v3 project.

Then add the `showLiveOutput` setting. This was the thing that was broken in v3+MTP; in v2+VSTest it actually works:

```json
{
  "$schema": "https://xunit.net/schema/current/xunit.runner.schema.json",
  "showLiveOutput": true
}
```

(Merge with whatever existing settings the file already has.)

Add to `Ledger.Tests.csproj` so it gets copied to output:

```xml
<ItemGroup>
  <Content Include="xunit.runner.json" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

We're flipping `showLiveOutput` on from the start to validate that v2+VSTest gives you what v3+MTP didn't — visible `ITestOutputHelper` output even during passing runs. This is one of the small "yes, the tools work here" moments worth experiencing early.

### Step 4 — Port the production code from v3  `[x]`

Copy these files from `~/professional/projects/learn-xunit/src/Ledger/` to `~/professional/projects/learn-xunit-v2/src/Ledger/`:

- `Money.cs`
- `CurrencyCode.cs`
- `Account.cs`
- `AccountRepository.cs`

These are pure domain code — no xUnit dependencies, no framework references. They run identically against either v2 or v3 tests. No edits needed; the namespace (`Ledger`) is the same.

After copying, run `dotnet build src/Ledger/` to confirm the production project builds clean against .NET 10 + no framework deps. Success here means a **clean build** (compiles, no errors) — there are no tests to run yet; `src/Ledger` is pure domain code. Test execution doesn't start until Step 6. The build should succeed trivially.

### Step 5 — Write the smoke test  `[x]`

Create `tests/Ledger.Tests/SmokeTest.cs`:

```csharp
namespace Ledger.Tests;

public class SmokeTest
{
    [Fact]
    public void ArithmeticSmoke()
    {
        Assert.Equal(4, 2 + 2);
    }
}
```

Identical to the v3 tutorial's smoke. xUnit's `[Fact]` and `Assert.Equal` have the same signatures in v2 and v3.

Delete the `UnitTest1.cs` placeholder that the `dotnet new xunit` template generates.

### Step 6 — Verify  `[x]`

From the project root:

```
dotnet test
```

Should see one test pass. If discovery fails or the test doesn't appear in the output, the most likely cause is a missing or wrongly-versioned `Microsoft.NET.Test.Sdk` reference — compare to your production projects' csproj files.

**Also worth trying:** run with verbose output to confirm `showLiveOutput` works:

```
dotnet test --logger "console;verbosity=detailed"
```

You should see the test name displayed. `--logger` works here because you're on VSTest, not MTP. (This is the command that didn't exist in the v3 tutorial; here it's standard.)

### Step 7 — Capture decisions  `[ ]`

Update the **Decisions made** section above with the actual versions of `xunit`, `xunit.runner.visualstudio`, and `Microsoft.NET.Test.Sdk` that landed. Future-you will appreciate it.

---

## What carries over from v3 — and what doesn't

You did the v3 tutorial through Phase 5. Almost everything you learned transfers:

**Transfers unchanged:**
- Project layout (`src/`, `tests/`, csproj/sln structure)
- Smoke-as-canary discipline
- TDD red-green-refactor rhythm
- "Compile errors aren't valid red" rule
- Domain code (Money, CurrencyCode, Account, AccountRepository)
- IDE setup (Rider Path D)
- `global.json` .NET 10 pin

**Different in v2:**
- Package set: `xunit` 2.9.3 + `xunit.runner.visualstudio` + `Microsoft.NET.Test.Sdk` (instead of `xunit.v3.mtp-v2`)
- `OutputType=Library` (instead of `Exe`)
- No MTP-specific csproj properties (no `TestingPlatformDotnetTestSupport`, no `IsTestProject` toggle gymnastics)
- `dotnet new xunit` (not `xunit3`)
- VSTest runner, not MTP — so `dotnet test --logger`, `--filter`, etc. all work as the broader ecosystem documents

---

## Stretch (optional)

- **Compare csproj files side-by-side**: open the v3 project's `tests/Ledger.Tests/Ledger.Tests.csproj` and the v2 project's same file in a diff viewer. Note the absence of MTP-related properties in v2. The diff is the v3-vs-v2-at-the-build-system-layer.
- **Open Rider's Test Explorer** with this v2 project loaded. You should see tests populate immediately — no Phase 0 caveat about Test Explorer being broken for MTP. This is one of the "things just work" moments.

---

## Notes & questions

_Fill in as you go._

-
