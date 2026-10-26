# Barbatos.Wpf — working agreement

A family of NuGet packages that bring proven application-development ergonomics to plain,
unpackaged desktop WPF. Everything here is a **library**: public API is the product, and a
careless signature is a breaking change for someone else's app.

**How guidance reaches you.** This file is always loaded and holds only what every task needs.
Task playbooks live in [`.claude/skills/`](.claude/skills/): when a task touches a package's API,
its tests or docs, or a repo workflow, load the matching skill before you start (index at the
bottom). Conventions for a kind of file live in [`.claude/rules/`](.claude/rules/) and are attached
when you open a matching file with the Read tool — a file seen only through Bash (`cat`, `grep`, a
shell loop) or created with Write may not attach its rule. [`.claude/README.md`](.claude/README.md)
explains the layout.

---

## 1. Repository map

```
src/        the four libraries, one folder per NuGet package (+ README.md, API-REFERENCE.md)
samples/    one runnable sample app per package + Barbatos.Wpf.Samples.Shared
tests/      one xUnit project per package
templates/  dotnet new templates (aq-view, aqv-form, barbatos-wpf-app)
build/      package icons
.claude/    agent rules, skills and their evals
```

| Package | TFMs | Depends on | Owns |
| --- | --- | --- | --- |
| `Barbatos.Wpf.Core` | `net{8,9,10}.0-windows10.0.17763.0` | MEDI + configuration/options/logging + Hosting.Abstractions, MEAI, MCP SDK, Agent Framework, OpenAI, Anthropic, System.ClientModel, SignalR client, Castle.Core, CommunityToolkit.Mvvm, Toolkit.Uwp.Notifications, System.Management | Hosting, LifecycleEvents, Dispatching, Essentials (namespaces `ApplicationModel`, `ApplicationModel.Communication`, `Devices`, `Devices.Sensors`, `Networking`, `Storage` — there is no `Barbatos.Wpf.Essentials` namespace), Dialogs, Tray, Power, Startup, Input, Notifications, PushNotifications, SingleInstance, Apsu, Mcp (+ `Mcp.Providers`) |
| `Barbatos.Wpf.Aquarius` | `net{8,9,10}.0-windows` | **`CommunityToolkit.Mvvm` + `Microsoft.Extensions.DependencyInjection.Abstractions` + `Options` only** | Reactivity, Composition, Xaml, Animation |
| `Barbatos.Wpf.AquariusRouter` | `net{8,9,10}.0-windows` | → Aquarius (ProjectReference), same three packages | Matching, Routing, Searching, Xaml |
| `Barbatos.Wpf.AquariusValee` | `net{8,9,10}.0-windows` | → Aquarius (ProjectReference), `CommunityToolkit.Mvvm` | Validation, Rules, Xaml |

**Aquarius has no dependency on Core** and must stay that way — it is installable standalone.
The two `src/` → `src/` references (Router → Aquarius, Valee → Aquarius) were each confirmed
with the maintainer before being added; do not add a third without asking.

All four share the `Barbatos.Wpf` root namespace. XAML namespaces:

| Prefix | URI |
| --- | --- |
| `aq` | `http://schemas.barbatos.co/aquarius/2026/xaml` |
| `aqr` | `http://schemas.barbatos.co/aquariusrouter/2026/xaml` |
| `aqv` | `http://schemas.barbatos.co/aquariusvalee/2026/xaml` |

---

## 2. Build and test

WPF builds, runs and tests on **Windows only**. In a Linux or macOS environment (a cloud session,
a container) you cannot run the build or the tests — say what you could not verify rather than
reporting it as verified.

```bash
dotnet build Barbatos.Wpf.slnx -c Debug
```

`GeneratePackageOnBuild` is on for every `src/` project, so a plain build also produces packages.

> **Never judge a change from a bare `dotnet test`.** Both TFMs run in parallel against one
> real per-user store (`Preferences`/`SecureStorage` land in `%LocalAppData%`), and the WPF
> STA/`Application.Current` helpers are process-wide. A combined run produces spurious
> failures — and a full-suite run is known to abort on some machines, which reads as a false
> green.

Run one project and one TFM at a time, and narrow to a class when investigating:

```bash
dotnet test tests/Barbatos.Wpf.Core.UnitTests -f net10.0-windows10.0.17763.0
dotnet test tests/Barbatos.Wpf.Aquarius.UnitTests -f net8.0-windows --filter "FullyQualifiedName~KeepAliveTests"
```

Core's test project uses the SDK-suffixed TFMs, so `-f` must name them exactly — a bare
`net10.0-windows` fails with `NETSDK1005`. The other three test projects use the bare form
(`net8.0-windows`, `net10.0-windows`).

```bash
dotnet run --project samples/Barbatos.Wpf.Core.Sample
```

---

## 3. Non-negotiables

The file-specific detail behind each of these loads with the matching rule
(`library-code`, `csharp-style`, `tests`, `build-and-packages`).

- **.NET 8, 9, 10 only.** Never add a `netstandard`, `net6` or `net7` target or a shim for one.
  Consumers of **Core** must use the SDK-suffixed TFM (`net10.0-windows10.0.17763.0`) — the bare
  form resolves to `windows7.0`, which has no WinRT projections and no Core asset, so restoring
  Core fails (`NU1201` through a `ProjectReference`, `NU1202` through a `PackageReference`).
- **C# 14 everywhere, three runtimes underneath.** Compiler-only features (`field`, extension
  members, collection expressions, primary constructors, `required`, …) are fine on every TFM;
  anything whose *type* ships with the runtime (`System.Threading.Lock`) needs
  `#if NET9_0_OR_GREATER` or is not used.
- **Every feature is reachable through DI.** Core: `ConfigureXxx(this WpfAppBuilder, …)` binding
  `Barbatos:Xxx` (file values override code). Aquarius and Router: `AddAquarius` /
  `AddAquariusRouter(IServiceCollection, …)` declare, `UseAquarius` / `UseAquariusRouter`
  (`IServiceProvider`) wire after `Build()`; they never own a container. Valee has no `Add`/`Use`
  pair — a `Form` is plain state its ViewModel constructs. Replaceable services use `TryAdd*`. In
  Aquarius/Router/Valee, anything constructed for the app goes through `Setup.Resolve` — never a
  second construction path (Core cannot see `Setup`; it resolves from the container).
- **No `InternalsVisibleTo`.** If a test needs internal behaviour, that behaviour becomes real,
  documented public API, or the test uses the public surface.
- **Public API discipline.** XML docs on every public member with `<remarks>` for the *why*;
  new types are `sealed` (Apsu stores excepted), and a shipped unsealed type is never sealed later
  — that breaks subclasses; a pure `Validate()` on user-built descriptor/schedule types
  (`McpServerDescriptor`, `PeriodicSchedule`, …); a rename keeps the old member as `[Obsolete]`
  instead of deleting it.
- **Central Package Management.** All versions live in `Directory.Packages.props`. The one
  deliberate exception is the `barbatos-wpf-app` template's `.csproj`, which pins `Version=`
  because it is generated outside the repo. `Microsoft.Extensions.AI*` moves in lockstep with
  `ModelContextProtocol.Core`'s floor.
- **`.editorconfig` is the style authority** (except `var`: follow the code), including the MIT
  header on every new `.cs` file in `src/` and `tests/`.

---

## 4. Documentation is part of the change

Every package ships `src/<Package>/README.md` (concept-first walkthrough, worked examples,
rationale — packed into the `.nupkg` as the NuGet landing page) and
`src/<Package>/API-REFERENCE.md` (member-by-member signatures and exact behaviour, read on
GitHub). **A public API change is not done until both are updated**, plus the sample that
demonstrates it, the template if the scaffolded shape changed, the root `README.md` if a package
summary, the package list or the templates changed, the root `API-REFERENCE.md` if a namespace
or package was added or removed, and every file under `.claude/skills/` that names the changed
member or states its changed default, exception or timing (`grep -rn "<Member>" .claude/skills`)
— a stale skill is an instruction the next agent will follow. See `barbatos-docs-sync`.

---

## 5. Skills

Claude Code lists these automatically and loads one when the task matches; invoke one directly
with `/barbatos-…`. Several often apply together — see [`.claude/README.md`](.claude/README.md).

| Skill | Use it when |
| --- | --- |
| [`barbatos-wpf-host`](.claude/skills/barbatos-wpf-host/SKILL.md) | Composing `WpfApp`, DI/config/lifecycle, tray, single instance, startup, keep awake, notifications, push, periodic services, shortcuts/hotkeys, dialogs, splash |
| [`barbatos-wpf-essentials`](.claude/skills/barbatos-wpf-essentials/SKILL.md) | `AppInfo`, `Preferences`, `SecureStorage`, `VersionTracking`, `Connectivity`, device/display info, email, launcher, jump list, permissions, licensing |
| [`barbatos-aquarius-view`](.claude/skills/barbatos-aquarius-view/SKILL.md) | Any View/ViewModel using `aq:` — reactivity, `Setup`, lifecycle hooks, directives, Teleport, Transition, Suspense, Slots, KeepAlive |
| [`barbatos-aquarius-router`](.claude/skills/barbatos-aquarius-router/SKILL.md) | Route tables, guards, `RouterView`/`RouterLink`, nested/named/modal routes, in-app search |
| [`barbatos-valee-form`](.claude/skills/barbatos-valee-form/SKILL.md) | Forms and validation — `Form`/`Field<T>`, DataAnnotations, cross-field rules, `aqv:` markup |
| [`barbatos-apsu-store`](.claude/skills/barbatos-apsu-store/SKILL.md) | State management — `StoreBase`, `Getter<T>`, `[Action]` interceptors, plugins |
| [`barbatos-mcp-ai`](.claude/skills/barbatos-mcp-ai/SKILL.md) | MCP servers, BYOK chat, agents, tool approval, model catalog, OAuth |
| [`barbatos-package-authoring`](.claude/skills/barbatos-package-authoring/SKILL.md) | Adding or changing public API in `src/`, csproj/build/packaging, versioning and release |
| [`barbatos-wpf-testing`](.claude/skills/barbatos-wpf-testing/SKILL.md) | Writing or running tests — STA threading, dispatcher pumping, GC leak probes |
| [`barbatos-docs-sync`](.claude/skills/barbatos-docs-sync/SKILL.md) | Updating `README.md` / `API-REFERENCE.md` after an API change |
| [`barbatos-templates`](.claude/skills/barbatos-templates/SKILL.md) | The `dotnet new` templates — using, testing, changing or adding one |
