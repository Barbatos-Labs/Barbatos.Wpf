---
name: barbatos-wpf-testing
description: Write or run tests for the Barbatos.Wpf packages - xUnit conventions, the STA-thread and TestApplication helpers WPF objects require, dispatcher pumping, GcProbe reachability assertions for leak fixes, testing DI registrations and attached properties, and the per-target-framework run discipline. Consult this before running dotnet test at all, when adding a test project or class, and especially when a result looks wrong - a test that passes alone but fails in a full run, a suite that aborts halfway through, an STA or dispatcher error, or a suspected memory leak where an object is not being collected. The per-TFM rule here is what separates a real failure from an artifact of the run, so check it before trusting or reporting any test result. Vietnamese prompts land here too: chạy test, viết unit test, test lúc chạy được lúc không, test chạy riêng thì pass chạy chung thì fail, rò rỉ bộ nhớ, lỗi STA hoặc dispatcher.
---

# Testing Barbatos.Wpf

One xUnit project per package under `tests/`, each multi-targeting `net8.0-windows` and
`net10.0-windows`. `Barbatos.Wpf.Core.UnitTests` uses the SDK-suffixed TFMs
(`net8.0-windows10.0.17763.0`/`net10.0-windows10.0.17763.0`), and `-f` must name them exactly.

---

## Running tests — read this before believing a result

> **A bare `dotnet test` at the solution level is not trustworthy here.**
>
> Both TFMs run in parallel against one real per-user store (`Preferences` and `SecureStorage`
> write to `%LocalAppData%`), and the WPF helpers below share one process-wide
> `Application.Current`. A combined run produces failures that are artifacts of the run, not the
> change. Separately, a full-suite run is known to **abort** on some machines — which reads as a
> false green.

Run one project, one TFM:

```bash
dotnet test tests/Barbatos.Wpf.Core.UnitTests -f net10.0-windows10.0.17763.0
```

Narrow further while investigating:

```bash
dotnet test tests/Barbatos.Wpf.Aquarius.UnitTests -f net8.0-windows --filter "FullyQualifiedName~KeepAliveTests"
```

When a test fails, **re-run that class alone on that single TFM before treating it as real.**
Two known-flaky-in-combination areas: `Barbatos.Wpf.Core.UnitTests` (the shared per-user store)
and `AquariusValee`'s `ValeeXamlTests` (STA/`Application` interaction — a different cause, same
remedy). Before judging a change, confirm the failure reproduces isolated.

---

## Conventions

- **Plain xUnit assertions** — `Assert.Equal`, `Assert.Throws`, `Assert.Same`, `Assert.Single`.
  `AwesomeAssertions` is referenced but not used anywhere; do not introduce `.Should()` into a
  file that does not already have it.
- One test class per type under test, named `<Type>Tests`.
- Method names are full sentences describing the behavior:
  `SettingTheSameValueDoesNotRaisePropertyChanged`,
  `ATeleportThatSawASecondLoadedIsStillCollectableAfterUnloading`.
- Arrange / act / assert separated by blank lines, no comment headers.
- A comment above a test explains *why the behavior matters* or *what bug it pins* — not what
  the code does.
- Package-specific namespaces come from `<Using Include="..."/>` in the test `.csproj`; add
  there rather than repeating `using` lines.
- MIT file header on every test file.

---

## WPF objects need an STA thread

Dependency properties, controls, and windows all require STA. Two helpers, and they are **not**
interchangeable.

**`StaThread.Run(body)`** — a fresh STA thread per test. Use it for anything that does not touch
`Application.Current`.

```csharp
[Fact]
public void ShowIsAppliedWhenTheBindingUpdates() => StaThread.Run(() =>
{
    var border = new Border();
    Directives.SetShow(border, false);

    Assert.Equal(Visibility.Collapsed, border.Visibility);
});
```

**`TestApplication.Invoke(body)`** — one long-lived STA thread with a running `Dispatcher`,
shared for the whole run. Use it for **anything that touches `Application.Current`**.

`Application.Current` is a process-wide singleton bound to whichever thread first constructed it.
A fresh `StaThread.Run` thread works only for the *first* test that touches it; every later test
finds `Application.Current` already set but pointing at a dispatcher whose thread has exited, so
anything posted to it never runs. Route every `Application`-touching test through
`TestApplication`.

**`StaThread.PumpDispatcher()`** blocks until everything queued at `DispatcherPriority.Background`
or higher has run — including WPF's own `Loaded`-priority tree-connection work. Call it after
anything that defers: `NextTick`, `WatchFlush.Post`, `IOnUpdated` batching, `FormMeta`
notification, adding an element to a tree.

Static-facade races: `Barbatos.Wpf.Core.UnitTests/ModuleInit.cs` warms every Essentials singleton
in a `[ModuleInitializer]` because MAUI's `field ??= new Impl()` lazy pattern is not thread-safe
on first access and xUnit runs classes in parallel. **Add any new Essentials facade there.**

---

## Leak tests

`GcProbe` is the only GC-based assertion mechanism here. Use it when the bug is "something still
holds a strong reference" and nothing about it is observable through the type's own API.

```csharp
[Fact]
public void ATeleportThatSawASecondLoadedIsStillCollectableAfterUnloading() =>
    StaThread.Run(() => GcProbe.AssertCollected(RunTeleport(loadedCount: 2), "the Teleport"));

[Fact]
public void ATeleportStillLoadedIsDeliberatelyNotCollectable() =>
    StaThread.Run(() => GcProbe.AssertAlive(RunTeleport(loadedCount: 1, unload: false), "a still-loaded Teleport"));
```

Three rules, all of which are the difference between a real test and one that passes for the
wrong reason:

1. **The subject must never be reachable from the calling frame.** Build it and take the
   `WeakReference` inside a separate `[MethodImpl(MethodImplOptions.NoInlining)]` helper. In
   Debug builds a local is rooted for the whole method body, which would fail every assertion for
   the wrong reason.
2. **Root what is supposed to stay alive.** Return the still-open `Window` (or whatever owns the
   live tree) so the caller keeps it rooted across the assertion — otherwise an unrooted
   `KeepAlive` is collected along with everything it caches and the two cases stop being
   comparable.
3. **Always pair `AssertCollected` with an `AssertAlive` control case.** Without one you have not
   shown the probe can fail. `LifecycleLeakTests` does this for every hazard it covers.

`AssertCollected` retries five times with a full blocking compacting collection, a finalizer
drain, and a `PumpDispatcher()` between attempts — WPF can still be holding the graph on the
first pass. A genuinely leaked object fails all five just as reliably as one.

The recurring hazard this exists for: **WPF raises `Loaded` a second time with no `Unloaded` in
between** (a `TabControl` realizes every tab up front, then raises `Loaded` again on the one that
becomes selected). A control that subscribes on `Loaded` and unsubscribes on `Unloaded` then
leaves one subscription behind — and against a `static` event that is a process-lifetime root.
When reviewing or writing lifecycle code: **guard both edges, and always re-hook what you
unhook.**

---

## What to test

| Surface | How |
| --- | --- |
| Reactivity (`Ref`/`Computed`/`Watch`) | plain objects, no STA needed |
| Attached properties / controls | `StaThread.Run` + direct `SetXxx`/assert |
| Anything reading `Application.Current` | `TestApplication.Invoke` |
| Deferred behavior (`NextTick`, `Post` flush, `IOnUpdated`) | act, then `PumpDispatcher()`, then assert |
| DI entry points (`AddXxx`/`ConfigureXxx`) | build a `ServiceCollection`, assert the descriptors and that `TryAdd` lets a prior registration win |
| Navigation pipeline | headless — `Router` never touches `Window`/`Application`, so guard order, failures and redirects need no UI at all |
| Input shortcuts | `IInputSystemService.ProcessKeyDown`/`ProcessKeyUp` — no window, no keyboard |
| Schedules | `PeriodicSchedule.GetNextOccurrence(now, lastRun)` is pure and never reads the clock — pass the time in |
| Options `Validate()` | assert the exact exception type and that it does no I/O |
| Leak fixes | `GcProbe`, per the rules above |

Prefer the headless seam when one exists — `ProcessKeyDown`, `GetNextOccurrence`, `Validate`,
and `Router`'s whole pipeline all exist partly so tests need no UI.

---

## Adding a test project

Match the existing csproj exactly: `TargetFrameworks` = `net8.0-windows;net10.0-windows`,
`UseWPF`, `IsTestProject`, `IsPackable=false`, the shared package references from
`Directory.Packages.props` (no versions), and `<Using Include="..."/>` for the package's
namespaces plus `Xunit`. Copy `StaThread.cs`/`TestApplication.cs`/`GcProbe.cs` from
`Barbatos.Wpf.Aquarius.UnitTests` rather than reinventing them.

**Never add `InternalsVisibleTo`.** If a test needs internal behavior, that behavior must become
real public API first — see `barbatos-package-authoring`.
