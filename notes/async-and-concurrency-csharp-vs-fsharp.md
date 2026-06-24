# Aside — Asynchrony & concurrency: C# vs F# (a pointer, not a tutorial)

> A reference capture, not a phase. Spawned from a Phase 6 (async tests) tangent. The point is to have a durable pointer to the *shape* of this space; depth lives in the linked references. **As-of:** mid-2026 (.NET 10, C# 14, F# 9-ish). Version-sensitive claims are dated inline.

## Why this matters *now* (even though it's been here since .NET 1)

The mechanisms below are old — APM shipped in .NET 1.0 (2002), F# async workflows ~2007. But for most of that time asynchrony and multicore were a *curiosity* for most application developers: single-threaded, synchronous code was "good enough" because single-core clock speeds kept rising. That stopped.

- **The free lunch ended.** ~2005, CPU makers hit power/heat walls and pivoted from faster cores to *more* cores. Herb Sutter's "The Free Lunch Is Over" (Dr. Dobb's, 2005) is the canonical marker. Performance gains now require *explicit* concurrency.
- **Cloud + services made everything I/O-bound.** Web/microservice workloads spend most of their time waiting on network/DB, not computing. Blocking a thread per request doesn't scale; non-blocking asynchrony does. This is what pushed `async/await` from "nice" to "required."

So the relevance is recent even though the tooling is old. That gap — long-present mechanism, recent broad necessity — is itself the interesting bit, and why it's worth understanding rather than cargo-culting.

## Terminology (these are orthogonal, and constantly conflated)

- **Asynchrony** — not *blocking a thread while waiting* (usually for I/O). One thread can juggle thousands of in-flight async operations with zero extra threads. `async/await` is primarily *this*.
- **Parallelism** — using *multiple cores* to do work at once (CPU-bound). TPL, `Parallel.For`, PLINQ.
- **Concurrency** — *structuring* a program as independent activities that may interleave (whether or not they run in parallel). Actors/message-passing live here.

`async/await` ≠ multithreading. Keeping these three separate dissolves most confusion in this area.

## C# — the long march to `async/await`

A sequence of models, each subsuming the last, finally settling on one:

1. **Threads** (`System.Threading.Thread`) — manual, heavyweight.
2. **APM** (.NET 1.0) — `BeginXxx`/`EndXxx` + `IAsyncResult`. Callback soup.
3. **EAP** (.NET 2.0) — `XxxAsync` methods + `XxxCompleted` events.
4. **TPL** (.NET 4.0) — `Task`/`Task<T>` as the unifying "future value"; `Parallel`/PLINQ for data parallelism.
5. **`async/await`** (C# 5, 2012) — compiler sugar that rewrites a method into a state machine over `Task`. The **TAP** pattern C# standardized on.

**Defining trait: a C# `Task` is *hot*** — already running the moment it exists. `await` schedules a continuation for its completion. `async/await` is a *bespoke compiler feature* bound to `Task`-like (awaitable) types.

## F# — got there first, and more generally

- **F# async workflows (`async { }`) shipped ~2007**, *years before* C# 5, and **directly inspired C#'s `async/await`** (Don Syme's work fed the C# language design). The lineage runs F# → C#, not the reverse.
- **It's one instance of a general feature.** `async` is a *computation expression* (CE); F# also ships `seq`, `task`, `option`, `result`, `query`, and supports **user-defined** CEs. Inside any of them you use the "bang" operators — `let!` (bind ≈ `await`), `do!`, `return!`, `use!`, `match!`. So F# has a *general monadic-workflow mechanism* and async is just one use of it. C#'s nearest analog is LINQ query syntax — far narrower.
- **F# `Async<'T>` is *cold*** — a *description* that doesn't start until you run it (`Async.RunSynchronously`, `Async.Start`, `Async.StartAsTask`). Composable, restartable; the opposite of C#'s eager hot `Task`. Biggest semantic gap.
- **Cancellation is ambient** — the `CancellationToken` flows implicitly through the workflow; C# threads it through every call by hand.
- **Convergence — `task { }` (F# 6, 2021):** produces a real .NET `Task` with a resumable state machine (same machinery as C#), for seamless C#/TAP interop and lower allocation. So modern F# has *both*: idiomatic cold `async { }` and hot, interop-friendly `task { }`. The `task` CE is the one that behaves like C#'s `await`.

## Concurrency *philosophy*, not just syntax

Both sit on the identical .NET runtime (ThreadPool, `Task`, TPL, `System.Threading`), so the primitives are shared. The cultures differ:

- **C#:** shared mutable state guarded by `lock`/`Interlocked`; `Task`/TPL/`Parallel`/PLINQ/channels for parallelism. Correctness is programmer discipline.
- **F#:** **immutability by default** (most threading bugs are mutation races — remove the mutation, remove the bug) + **message passing**.

## Shout-out: the actor model & F# `MailboxProcessor`

The actor model (Carl Hewitt, 1973; industrialized by Erlang/OTP at Ericsson, ~1986; carried forward by Elixir, Akka/Akka.NET, and MS Orleans's "virtual actors") is a fundamentally different stance on concurrency: **share nothing; communicate by messages.** Each actor owns private state and processes an inbox of messages *one at a time, sequentially* — so there are no locks, because there's no shared mutable state to guard.

F# ships this in the box as **`MailboxProcessor<'Msg>`** (the "agent"): a lightweight mailbox + a sequential message loop, often wrapping mutable state safely behind an async receive (`let! msg = inbox.Receive()`). C# has no built-in equivalent — you reach for channels, TPL Dataflow, Akka.NET, or Orleans.

**An honest take on "is it simpler?"** The intuition that actors are a *simpler, more obvious* concurrency model has real merit — but it's sharpest for a specific problem: **safe *stateful* concurrency.** "One message at a time against private state" eliminates the entire class of lock-ordering/race bugs by construction, and it scales to distributed systems (Erlang's "let it crash" + supervision trees are arguably the most battle-tested concurrency story in industry). Where it's *not* automatically simpler:

- For pure **I/O asynchrony** (await a DB call, return), an actor is heavier ceremony than a plain `async`/`task` — async/await wins on directness there.
- For **fan-out CPU parallelism** (map over a big array across cores), `Parallel`/PLINQ is more direct than modeling it as actors.
- Actors trade compile-time type safety at the boundary (messages are often a discriminated union you pattern-match — F# makes this nicer than most) for runtime flexibility, and introduce their own hazards (mailbox backpressure, ordering assumptions, deadlock-by-request-reply).

They're **not competitors** — they compose. The common pattern is an agent whose message handler `await`s I/O internally: actors for *safe stateful concurrency*, async/await for *non-blocking waiting*, parallelism for *CPU throughput*. Picking the axis your problem actually lives on is most of the skill. (Your Erlang/Prolog/Elixir/Akka.NET/`MailboxProcessor` background is exactly the right lens — the instinct that message-passing is "obvious" is well-earned; the nuance is just that it's *obvious for the stateful-concurrency axis specifically*.)

## Four composable layers (and a myth to retire)

**Myth to retire:** *"`async/await` is for I/O; F#'s `async {}` is for multithreading."* Not true. Both `async/await` (C#) and the `async {}` / `task {}` **computation expressions** (F#) are **asynchrony** abstractions — they compose continuations over operations that finish later. Neither is intrinsically a *threading* tool. F#'s "more general" quality is about the CE *mechanism* (general monadic workflows), not CPU parallelism. The actual parallelism tools (TPL/`Parallel`/PLINQ/thread pool) are **shared by both languages**.

The unifying insight: **`Task<T>` represents "a value available later" regardless of whether the latency is I/O or computation.** An `await` suspends until *something* signals completion and is agnostic to the cause — `await ReadAsync()` (I/O, IOCP-driven) and `await Task.Run(() => Compute())` (CPU, thread-pool-driven) resume through the *same* machinery. So "continuations that handle both I/O and CPU" isn't a future adjustment — it's the existing design (TPL unified them in .NET 4.0; `async/await` layered composition on top in C# 5).

The toolbox is four layers that **compose** — pick by the axis your problem lives on:

| Need | Tool |
|---|---|
| Compose "completes later" (I/O *or* CPU) | `async/await` / `Task` (C#); `async {}` / `task {}` (F#) |
| Use multiple cores (CPU throughput) | `Task.Run` / `Parallel` / PLINQ (both languages) |
| Safe *stateful* concurrency | actors — `MailboxProcessor` (F#), Akka.NET / Orleans |
| Coordinate flows between producers/consumers | **CSP channels** — `System.Threading.Channels`; Go |

### What `async/await` does *not* solve

- **It doesn't *parallelize* by itself.** `await` composes; the parallelism comes from the scheduler/thread pool. `await`ing CPU work just means "don't block while it runs elsewhere" — you still need `Task.Run`/`Parallel`/PLINQ to actually use cores.
- **Function coloring.** `async` infects every caller up the chain (Bob Nystrom, "What Color Is Your Function?", 2015). The industry's proposed cure is **green/virtual threads** (Java's Project Loom: make blocking cheap, drop the coloring). **.NET ran a green-threads experiment (~2023) and chose *not* to ship it**, citing the cost of splitting its deep async-invested ecosystem. So the "make it uniform / remove the color" instinct is real — .NET just declined it, for now.

### Where CSP fits

**CSP** (Communicating Sequential Processes — Tony Hoare, **1978**) is a *third* paradigm, distinct from actors:
- **Actors** (Hewitt): async messages to a *named* entity with a mailbox; identity matters; delivery is buffered.
- **CSP** (Hoare): *anonymous* processes over **channels**, classically a synchronous rendezvous; you send to a *channel*, not an actor.

Practically: **Go** is the canonical CSP language (goroutines + channels + `select`); **.NET's embodiment is `System.Threading.Channels`** (`Channel<T>`, bounded/unbounded, backpressure), which composes naturally with `async/await` (`await channel.Reader.ReadAsync()`). CSP is the **coordination/pipeline layer** — decoupling producers from consumers, backpressure, fan-in/fan-out — sitting *on top of* async/await. A channel consumer that `await`s I/O inside an actor is perfectly normal; the four layers stack.

## Clojure — the most deliberate menu (and the one with STM)

Clojure is worth a section because it's the most *intentional* of these languages about concurrency, and it owns one mechanism the others lack built-in: **software transactional memory**.

**Foundation — identity vs. state vs. value.** Like F#, Clojure is immutable-by-default (persistent data structures). Rich Hickey's framing (talks: *Are We There Yet?*, *The Value of Values*) is the key idea: a **value** never changes; **state** is a *succession* of values over time; an **identity** is a stable reference that points to different values at different moments. Concurrency then becomes "how do I coordinate *changing which value an identity points to*," and Clojure gives you a **menu of reference types chosen by semantics** rather than one hammer:

| Reference type | Coordination | Timing | Mechanism |
|---|---|---|---|
| **Atom** | uncoordinated (one identity) | synchronous | lock-free compare-and-swap (`swap!`) |
| **Ref** | **coordinated** (many identities, atomically) | synchronous | **STM** — `dosync` transactions |
| **Agent** | uncoordinated | **asynchronous** | action queued, applied serially on a thread pool (`send`) |

**STM (the standout)** — `ref` + `dosync` give you database-style transactions *for memory*: change multiple refs atomically, with no manual locks. Transactions are speculative and **auto-retry on conflict** (MVCC). This is feasible precisely *because* values are immutable — taking a consistent snapshot is cheap (persistent data structures), and retrying is safe as long as the transaction body is pure (a real caveat: side effects inside `dosync` may run more than once). Clojure is the notable mainstream language to ship STM in the box (the idea traces to STM research and Haskell's `STM` monad).

**core.async = Clojure's CSP** — the `core.async` library (2013) brings Go's model: **channels** (`chan`), **`go` blocks**, `<!`/`>!` (parking take/put, only inside a `go`), `<!!`/`>!!` (blocking, anywhere), and `alt!`/`alts!` (≈ Go's `select`). The neat tie-back to this whole document: a **`go` block is a *macro* that rewrites your sequential code into a state machine over callbacks** — *conceptually the same trick* as C#'s `async/await` compiler transform and F#'s `task {}` CE, just done in userland via Lisp macros rather than by the compiler. And those parked `go` processes are green-thread-like (multiplexed onto a small fixed pool) — the very thing .NET's shelved green-threads experiment was chasing.

**One precision worth stating** (you know actors well, so this matters): **Clojure "agents" are *not* the actor model.** You `send` an agent a *function* to apply to its current value — not a *message/protocol* to a *named, addressable* process with a mailbox (Erlang/Akka/`MailboxProcessor`). Agents are "asynchronous, uncoordinated state," not message-passing entities. So Clojure's actual *actor* story, if you want one, is a library (e.g., via core.async channels, or Pulsar/quasar), not the built-in `agent`.

**Net:** where F# says "immutability + actors + async," Clojure says "immutability + *pick the reference type that matches your coordination need* + STM for the coordinated case + core.async for CSP." It's the same anti-shared-mutable-state instinct, expressed as a richer, more explicitly-classified toolkit — and the only one here that treats coordinated multi-identity updates as a first-class, lock-free, transactional problem. (Clojure also has the full JVM underneath: `future`, `promise`, `pmap`, `java.util.concurrent`.)

## Hot vs cold: futures across languages

The single most clarifying axis for an "async value" type — and the one that surprises people coming from C# — is **when does the work start?**

- **Hot / eager** — *creating* the future *starts* the work. Construction **is** execution. You hold a handle to something already in flight.
- **Cold / lazy** — the future is a **description** that does nothing until you explicitly run it. Like an ordinary function: inert until invoked.

The counterintuitive bit: **cold is the "normal," function-like, referentially-transparent model** — a value you can pass around and compose *without* triggering anything, run once at the edge. **Hot is the odd one out**: merely *constructing* it has a side effect (it starts running), which breaks referential transparency. The functional camp considers hot futures a wart for exactly this reason; the imperative camp likes them because "call it and it just goes" is ergonomic.

Where the mainstream **future-value types** land *by default*:

| Language / type | Default | Notes |
|---|---|---|
| **C# `Task` / `Task<T>`** | **hot** | runs on creation; `await` only *waits*. |
| **JavaScript `Promise`** | **hot** | `new Promise(exec)` runs `exec` immediately; an `async` fn starts on call. |
| **Scala `Future`** | **hot** | schedules on an `ExecutionContext` at creation. |
| **Java `CompletableFuture` / `Future`** | **hot** | `supplyAsync` / `submit` start running. |
| **Rust `Future`** | **cold** | *"futures are lazy"* — do nothing until polled/`.await`ed or spawned. A deliberate reaction against hot futures. |
| **F# `Async<'T>`** | **cold** | a description; `Async.RunSynchronously` / `Start` to run. (F# 6 `task {}` opts into **hot** for .NET interop.) |
| **Kotlin `suspend`** | **cold** | runs when a coroutine builder (`launch`/`async`) drives it. |
| **Python `async def` coroutine** | **cold** | calling it returns an un-started coroutine; `await` / `asyncio.create_task` (→ hot `Task`) runs it. |
| **Cats Effect `IO` / ZIO (Scala)** | **cold** | referentially-transparent effect values; run at the "end of the world." |
| **Haskell `IO`** | **cold** | effects are *values*; `main` runs them. |
| **Clojure** | **both** | `(delay …)` cold (thunk, forced with `@`); `(future …)` hot (thread pool, now); `promise` a write-once cell. One language, both, explicitly named. |

**Two camps, their trade-offs:**

- **Hot (C#, JS, Scala `Future`, Java):** ergonomic — "make it, await it." Costs: construction isn't pure (a side effect), you can't *re-run* one (re-create it instead → which is *why* retry/poll APIs take a `Func<Task<T>>`, not a `Task<T>`), and cancellation/resource-safety are bolted on.
- **Cold (Rust, F# `Async`, Cats Effect/ZIO, Haskell, Kotlin):** referentially transparent — a future is a *value* you compose freely; trivial retry (run it again); first-class composable cancellation and resource safety. Costs: needs a runtime/executor, more ceremony, and the classic beginner gotcha — *"why isn't my future running?"* (Rust newcomers hit this constantly).

**The bridge is symmetric** — hot and cold inter-convert by adding/removing a function wrapper:

- **cold = `() => hot`** — wrap a hot future in a thunk to defer it: C# `Func<Task<T>>`, JS `() => fetch(…)`. This is why retry/scheduling APIs want a *factory*, not a started task.
- **hot = `run(cold)`** — execute a cold description: `Async.StartAsTask`, ZIO/`IO` `unsafeRun*`, Rust `tokio::spawn`.

So cold/hot isn't alien to general CS — it's the familiar *"a function is inert until invoked"* idea applied to async work. Hot futures are the special case that fused "construct" and "invoke" for convenience; everything else recovers laziness by re-introducing a function boundary.

### A different category: processes & coroutines (Go, core.async, Erlang/Elixir)

Hot/cold is really a property of *future-value* types. **Go goroutines, Clojure core.async `go` blocks, and Erlang/Elixir processes are not future-values at all** — they're *lightweight processes / coroutines* (the CSP and actor families). They don't hand back an awaitable handle; results travel over **channels** (CSP) or **messages** to a mailbox (actors). The only question that translates — "is it launched eagerly?" — answers uniformly **hot**:

- **Go** — `go f()` launches *now* (M:N green-thread scheduler); result returns via a **channel**.
- **Clojure core.async** — `(go …)` schedules on the core.async pool *now* and returns a **channel** for the result; cooperative *parking* via `<!`/`>!`. (The `go` macro is the same CPS/state-machine rewrite as `async/await` — userland, via macros.)
- **Erlang/Elixir** — `spawn` / `Task.async` start a process *immediately* (preemptive BEAM, isolated heaps); results arrive as **messages**. (Elixir's `Task.async` + `Task.await` is the one future-shaped convenience — a *hot* handle, much like C# `Task`.)

"Cold" for these is simply *a function you haven't spawned yet* (`() => spawn(...)`) — the same bridge. Their scheduling differs (Go: M:N green threads; core.async: cooperative coroutines; BEAM: preemptive isolated processes), but that's orthogonal to hot/cold. The clean way to hold it: **future-values** answer "when does the *value's* work start?" (hot/cold); **processes/coroutines** answer "how do independent activities *communicate*?" (channels/messages) — and are spawned hot.

## Reactive streams (Rx): async *sequences* of values over time

Everything above is about *one* async value (a future) or *communicating processes*. **Reactive** supplies the missing shape: **zero-to-many values arriving asynchronously over time** — an async *stream*.

**The core duality (Erik Meijer; Rx's origin at Microsoft).** `IObservable<T>`/`IObserver<T>` is the precise mathematical **dual of `IEnumerable<T>`/`IEnumerator<T>`** — flip pull into push:
- `IEnumerable<T>` — **pull**: the consumer asks for the next item (`MoveNext`).
- `IObservable<T>` — **push**: the producer pushes items to subscribers (`OnNext`…, then `OnCompleted`/`OnError`) as they arrive.

So Rx is **"LINQ over time"** — the same `Select`/`Where`/`Merge`/`Throttle`/`Buffer` combinators over a push stream. A `Task<T>` is the degenerate case: a stream of exactly one value (`task.ToObservable()`; convert back with `await obs` / `FirstAsync()`). The *pull*-based cousin in modern C# is **`IAsyncEnumerable<T>`** (C# 8, `await foreach`) — an async stream you pull; Rx is the push version.

**Hot vs cold — same axis, and this is where the terms were born:**
- **Cold observable** — starts producing *on subscribe*; each subscriber gets its own independent run (an HTTP call, `Observable.Interval`).
- **Hot observable** — produces *regardless* of subscribers; you see values only from when you subscribe (mouse events, a `Subject`, a price feed). `Publish()`/`RefCount()` convert cold → hot-shared.

**Why it let you "ignore threads" (your architecture).** Rx abstracts concurrency behind **`IScheduler`**. You compose the whole pipeline thread-agnostically, then inject threading *declaratively at the edges*: `SubscribeOn(scheduler)` (where the source work runs) and `ObserveOn(scheduler)` (where downstream notifications are delivered — e.g. `ObserveOn(uiScheduler)` to marshal back to the UI thread). That's precisely "threads only at the lowest level": operators don't care what thread they're on; you state threading once, at composition edges. **Testability bonus:** a `TestScheduler` gives you *virtual time* — fire "5 seconds later" deterministically in a unit test without waiting.

**Honest caveats:**
- **Backpressure.** Push-based Rx has no built-in flow control for fast-producer/slow-consumer. The JVM world answered with the **Reactive Streams** spec (`Publisher`/`Subscriber` + `request(n)`) — Project Reactor, Akka Streams, RxJava `Flowable`. In .NET, reach for `IAsyncEnumerable` or bounded `System.Threading.Channels` when backpressure matters.
- **"Everything is a stream" overreach.** Rx shines for event/UI/sensor/feed data and composing-throttling-merging async events; forcing one-shot request/response through it is ceremony, and debugging deep push pipelines is harder than stepping imperative code.

**Two meanings of "reactive" worth separating** (you said "reactive systems," described Rx):
- **Reactive *programming*** — Rx/observables/dataflow: the *programming model* (what you used).
- **Reactive *Systems*** — the *Reactive Manifesto* (responsive, resilient, elastic, **message-driven**): an *architecture* stance that overlaps the actor section above (Akka), not specifically Rx.

**Cross-language:** the **ReactiveX** family — RxJS, RxJava, RxSwift, Rx.NET (`System.Reactive`); **Project Reactor** (`Mono`/`Flux`, Spring WebFlux) and **Akka Streams** on the JVM; the academic root is **FRP** (Conal Elliott & Paul Hudak's *Fran*, 1997; later Elm).

**Where it fits the toolbox:** Rx is the **stream layer** — the async-*many* counterpart to Task's async-*one*. It composes with everything: a `Task` is a one-element observable; an Rx pipeline can sit over a channel or an actor's output; and `ObserveOn` is how it stays thread-agnostic until the edges. The trade-off your team made — adopt it early and architecturally — is what pays off: Rx rewards being the *spine* of the data flow, not a spot fix.

## Pointers (where the depth lives)

- Herb Sutter, **"The Free Lunch Is Over"** (Dr. Dobb's Journal, 2005) — the multicore inflection point.
- Don Syme, Tomas Petricek, Dmitry Lomov, **"The F# Asynchronous Programming Model"** (PADL 2011) — the cold/compositional async design that influenced C#.
- Stephen Cleary, **"There Is No Thread"** and his *Concurrency in C# Cookbook* — the canonical demystification of `async/await` ≠ threads.
- Carl Hewitt et al. (1973), **actor model**; Joe Armstrong, **Erlang/OTP** ("let it crash", supervision); Microsoft **Orleans** (virtual actors) docs.
- F# docs: **Async programming in F#**, **computation expressions**, and **`MailboxProcessor<'T>`**.
- Bob Nystrom, **"What Color Is Your Function?"** (2015) — the function-coloring problem `async` introduces.
- C. A. R. Hoare, **"Communicating Sequential Processes"** (CACM, 1978) — the theory behind channels; embodied in **Go** (goroutines/channels) and **.NET `System.Threading.Channels`**.
- The **.NET green-threads experiment** (dotnet/runtime, ~2023) and write-up on why it wasn't pursued — the road not taken vs. Java's Project Loom.
- Rich Hickey talks — **"Are We There Yet?"** and **"The Value of Values"** (identity/state/value, the epochal time model) — plus the Clojure docs on **refs/STM, atoms, agents** and the **`core.async`** announcement (2013).
- Erik Meijer, **Rx / "Your Mouse Is a Database"** (the `IEnumerable`↔`IObservable` duality) and **ReactiveX** (reactivex.io); the **Reactive Manifesto** (2014) and **Reactive Streams** spec (backpressure). FRP roots: Conal Elliott & Paul Hudak, **"Functional Reactive Animation"** (1997); **Elm**.

## Tie-back to the tutorial

When you write **xUnit tests for F# code**, xUnit awaits a `Task` — so return a `task { … }` block, or convert an `Async` with `Async.StartAsTask` / `Async.AwaitTask`. The Phase 6 `async Task` mechanics are the C# face of exactly this; `task { }` is the F# face returning the same `Task` the runner awaits.
