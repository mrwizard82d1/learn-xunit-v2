# Learn xUnit v2 — Tutorial Plan

> The version of xUnit you'll actually use at work.

## Why v2 (and not v3)

The original learn-xunit project targeted xUnit v3 with the Microsoft Testing Platform (MTP) runner — the bleeding edge. Working through Phases 0-5 surfaced several tooling-level problems:

- `--show-live-output` advertised in the runner's `--help` but rejected by the parser ([xunit issue #3468](https://github.com/xunit/xunit/issues/3468), closed as External / not planned).
- `--logger` and `--filter` (standard VSTest grammar that every .NET tutorial assumes) don't exist; MTP requires `-- --filter-trait` and friends, with the syntax differing from broader ecosystem documentation.
- The `xunit.runner.json` `showLiveOutput` setting subject to the same MTP pipeline issue.

A scan of the user's 360 C#/F# projects under `~/source/repos` showed **92 xUnit-using projects, 100% on xUnit v2**, zero v3, zero MTP. With no organizational migration plan and current v3+MTP rough edges, learning v3 first was building muscle memory for a version that isn't actually used in the user's day job.

This tutorial pivots to **xUnit v2 (2.9.3)** — the version that powers the user's actual production work, runs through VSTest as the broader .NET ecosystem assumes, and where the CLI knobs documented in every tutorial actually work.

The v3 tutorial (in `~/professional/projects/learn-xunit/`) is preserved as evidence of v3's current rough edges. It's a useful reference for *what not to recommend at work yet*, and the xUnit-vs-NUnit translation work in Phases 1-4 still applies almost unchanged.

## What's different from the v3 tutorial

Most content transfers. The substantive differences live in three areas:

- **Setup (Phase 0)**: Different package set, no MTP, different csproj shape.
- **Theory data serialization (Phase 3)**: v2 uses `IXunitSerializable` *on the type itself*; v3's external `IXunitSerializer` doesn't exist. Different pattern, same problem.
- **Conditional skip (Phase 5)**: v2 doesn't have `Assert.Skip*` native; the `Xunit.SkippableFact` package fills the gap.
- **CLI tooling (cross-cutting)**: VSTest grammar applies — `--filter`, `--logger`, `xunit.runner.json` settings all work as documented. Everything you read in mainstream .NET tutorials applies here.

Everything else — `[Fact]` and Assert (Phase 1), lifecycle (Phase 2), `[Theory]`/data attributes (Phase 3 main content), fixtures (Phase 4), output/traits/skip mechanics (Phase 5 main content), async (Phase 6), custom data, parallelism, integration testing, migration — applies essentially unchanged from the v3 tutorial.

## Goal

The user has done Phases 0-5 of the v3 tutorial. Not a beginner anymore. This tutorial:

- **Stands up a v2-correct project** mirroring the v3 one's structure.
- **Ports the Ledger domain code** unchanged (production code is v2-compatible).
- **Walks the v2-specific differences** explicitly, fast through what's unchanged.
- **Continues with new material** (Phases 6+) where the original tutorial wasn't reached yet.

The point is to land on a v2 working knowledge that matches what you'd encounter in production, not to repeat the v3 learning journey.

## Setup notes

- **Project root**: `~/professional/projects/learn-xunit-v2/`
- **Editor**: JetBrains Rider via Gateway Remote Development (Path D, same as the original learn-xunit project)
- **Runtime**: .NET 10 with `global.json` pin
- **Testing**: xUnit **v2.9.3** with VSTest runner via `xunit.runner.visualstudio`. No MTP. Rider's Test Explorer should work cleanly with this combination — the v3+MTP+Rider 2026.1 incompatibility doesn't apply.
- **Branch**: `2026-06` (yyyy-mm convention).

## Phases

- [x] Phase 0 — Setup (v2)
- [x] Phase 1-4 — Port (mechanical; combined doc)
- [ ] Phase 5 — Output, traits, skipping (v2)
- [ ] Phase 6 — Async tests done right
- [ ] Phase 7 — Custom test data and assertions
- [ ] Phase 8 — Parallelism, collections, and ordering
- [ ] Phase 9 — Integration testing patterns
- [ ] Phase 10 — Migration & coexistence

The "1-4 Port" phase consolidates four port-only walkthroughs. Each was a substantive multi-day undertaking in v3; in v2 the changes are concentrated enough that one focused doc covers them all.

## Domain: Ledger (continued from v3)

Continuing the Ledger / Money / Account / AccountRepository domain from the v3 tutorial. Production code copies over unchanged in Phase 0. The v3 tutorial's design decisions (CurrencyCode as a value type, Money as a record, accounts opened-not-added, repository owns ID generation, etc.) all carry forward — they're domain decisions, not framework decisions.

## Style

Same as the v3 tutorial:

- **TDD red-green-refactor** as the default rhythm. Smoke tests are permanent canaries — kept, not deleted.
- **Compile errors are not valid "red" states.** Every red must be a runnable assertion failure.
- **You type the code.** I describe what to write and why; no shell commands or scaffolding files on your behalf unless you explicitly grant.
- **Candidate test lists** in phase docs (Kent Beck's pattern). Decisions captured in each phase doc's "Decisions" section as we make them.
- **Translation notes where useful**: NUnit ↔ xUnit (continuing from the v3 tutorial), plus occasional **xUnit v3 ↔ v2** call-outs where you'd recognize a pattern from the v3 work that has a different shape here.

## Resume protocol

- Step headers end with `[ ]`. Flip to `[x]` when complete.
- First unchecked step is the resume point.
- "Notes & questions" at the bottom of each phase doc is yours — fill in as you go.

## Cross-project memory note

This project's Claude Code session will have its own memory directory (separate from `learn-xunit` and `learn-language-ext`). When you first invoke Claude here, you may want to seed memory with the equivalents of:

- Your TDD style and the "compile errors aren't reds" rule
- Your IDE preferences (Rider/IntelliJ keymap)
- Project setup (.NET 10, VSTest runner, Path D)
- The pivot rationale: this is the v2 tutorial because the org uses v2 exclusively and v3+MTP has too many current rough edges to recommend

Or re-tell me when relevant — your call. The fastest path is usually a single message at the start of a new session: "Same style as learn-xunit (the v3 one); this is the v2 port because org uses v2."
