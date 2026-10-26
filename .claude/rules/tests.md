---
paths:
  - "tests/**"
---

# Tests under `tests/`

One xUnit.net v3 project per package. Load `barbatos-wpf-testing` before writing a test or
trusting a result — it has the helpers' exact API and the leak-test rules.

## Running — never trust a bare `dotnet test`

Both TFMs run in parallel against one real per-user store (`Preferences` / `SecureStorage` in
`%LocalAppData%`), and the WPF helpers share a process-wide `Application.Current`, so a combined
run fails spuriously — and a full-suite run can abort, which reads as a false green. Run one
project and one TFM at a time:

| Project | `-f` values |
| --- | --- |
| `tests/Barbatos.Wpf.Core.UnitTests` | `net8.0-windows10.0.17763.0`, `net10.0-windows10.0.17763.0` (a bare `net10.0-windows` fails with `NETSDK1005`) |
| `tests/Barbatos.Wpf.Aquarius.UnitTests`, `…AquariusRouter.UnitTests`, `…AquariusValee.UnitTests` | `net8.0-windows`, `net10.0-windows` |

```bash
dotnet test tests/Barbatos.Wpf.Aquarius.UnitTests -f net8.0-windows --filter "FullyQualifiedName~KeepAliveTests"
```

A failure is real only once it reproduces with that class alone, on that one TFM.

## Writing

- WPF objects need STA. In the Aquarius-family projects use `StaThread.Run(...)` for plain
  controls and `TestApplication.Invoke(...)` for anything that touches `Application.Current` —
  they are not interchangeable. Each of those projects carries its own copy (Aquarius and Router
  also have `GcProbe`; Valee does not). For a new suite copy `TestApplication` from the Router or
  Valee project: the Aquarius copy lacks `ShutdownMode.OnExplicitShutdown`, so a test that closes
  the last window shuts the shared `Application` down for every later test.
- `Barbatos.Wpf.Core.UnitTests` has none of these helpers. A Core `Window` test uses a file-local
  `RunOnStaThread` (see `DialogServiceTests.cs`); timer tests pump
  `Barbatos.Wpf.Dispatching.Dispatcher.GetForCurrentThread()` with a `DispatcherFrame`
  (`InputChordTests.cs`). Follow that pattern there instead of calling `StaThread`.
- After anything deferred (`NextTick`, watcher flush, `IOnUpdated`, adding to a tree) call
  `StaThread.PumpDispatcher()` before asserting.
- Leak fixes are proven with `GcProbe.AssertCollected` **paired with** an `AssertAlive` control,
  building the subject in a separate `[MethodImpl(MethodImplOptions.NoInlining)]` helper.
- A new Essentials static facade gets warmed in `Barbatos.Wpf.Core.UnitTests/ModuleInit.cs`.
- Plain xUnit `Assert.*`; test classes are `<Type>Tests` or `<Type><Concern>Tests`
  (`RouterNavigationGuardOrderTests`); method names are sentences
  describing the behaviour; a comment says why the behaviour matters, not what the code does.
- Match the project's using style: the Aquarius-family test projects take package namespaces
  from `<Using Include="…"/>` in the `.csproj`; Core's test project globalizes only `Xunit` and
  `Barbatos.Wpf.Hosting`, and each Core test file writes its other namespaces as `using` lines.
- MIT file header on every new test file. Core's tests all carry it; **none** of the files in the
  three Aquarius-family test projects do, so copy it from `src/` or a Core test, not from a
  neighbour. Nothing catches a missing one at build time (`IDE0073` is not enforced in the build).
- **Never add `InternalsVisibleTo`.** Test through public API, or make the behaviour real public
  API first (`barbatos-package-authoring`).
