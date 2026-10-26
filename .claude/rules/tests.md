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

- WPF objects need STA: `StaThread.Run(...)` for plain controls, `TestApplication.Invoke(...)`
  for anything that touches `Application.Current` — they are not interchangeable. Each
  Aquarius-family test project carries its own copy of these helpers (and `GcProbe` where leak
  tests exist); copy them from `Barbatos.Wpf.Aquarius.UnitTests` rather than reinventing them.
- After anything deferred (`NextTick`, watcher flush, `IOnUpdated`, adding to a tree) call
  `StaThread.PumpDispatcher()` before asserting.
- Leak fixes are proven with `GcProbe.AssertCollected` **paired with** an `AssertAlive` control,
  building the subject in a separate `[MethodImpl(MethodImplOptions.NoInlining)]` helper.
- A new Essentials static facade gets warmed in `Barbatos.Wpf.Core.UnitTests/ModuleInit.cs`.
- Plain xUnit `Assert.*`; one `<Type>Tests` class per type; method names are sentences
  describing the behaviour; a comment says why the behaviour matters, not what the code does.
- Namespaces come from `<Using Include="…"/>` in the test `.csproj`.
- MIT file header on every test file.
- **Never add `InternalsVisibleTo`.** Test through public API, or make the behaviour real public
  API first (`barbatos-package-authoring`).
