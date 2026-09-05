# Barbatos.Wpf — working agreement

A family of NuGet packages that bring proven application-development ergonomics to plain,
unpackaged desktop WPF. Everything here is a **library**: public API is the product, and a
careless signature is a breaking change for someone else's app.

Read this file first. For task-shaped guidance, load the matching skill under
[`skills/`](skills/) — the index is at the bottom.

---

## 1. Repository map

```
src/        the four libraries, one folder per NuGet package
samples/    one runnable sample app per package + Barbatos.Wpf.Samples.Shared
tests/      one xUnit project per package
templates/  dotnet new templates (aq-view, aqv-form, barbatos-wpf-app)
build/      package icons
skills/     agent skills for this repo (see index below)
```

| Package | TFMs | Depends on | Owns |
| --- | --- | --- | --- |
| `Barbatos.Wpf.Core` | `net{8,9,10}.0-windows10.0.17763.0` | MEDI, MEAI, MCP SDK, Castle.Core, Toolkit.Uwp.Notifications, System.Management | Hosting, LifecycleEvents, Dispatching, Essentials, Dialogs, Tray, Power, Startup, Input, Notifications, PushNotifications, SingleInstance, Apsu, Mcp |
| `Barbatos.Wpf.Aquarius` | `net{8,9,10}.0-windows` | **`Microsoft.Extensions.DependencyInjection.Abstractions` + `Options` only** | Reactivity, Composition, Xaml, Animation |
| `Barbatos.Wpf.AquariusRouter` | `net{8,9,10}.0-windows` | → Aquarius (ProjectReference) | Matching, Routing, Searching, Xaml |
| `Barbatos.Wpf.AquariusValee` | `net{8,9,10}.0-windows` | → Aquarius (ProjectReference) | Validation, Rules, Xaml |

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

```bash
dotnet build Barbatos.Wpf.slnx -c Debug
```

Test projects multi-target `net8.0-windows` **and** `net10.0-windows`.

> **Never judge a change from a bare `dotnet test`.** Both TFMs run in parallel against one
> real per-user store (`Preferences`/`SecureStorage` land in `%LocalAppData%`), and the WPF
> STA/`Application.Current` helpers are process-wide. A combined run produces spurious
> failures — and a full-suite run is known to abort on some machines, which reads as a false
> green.

Run one TFM at a time, and narrow to a class when investigating:

```bash
dotnet test tests/Barbatos.Wpf.Core.UnitTests -f net10.0-windows
```

```bash
dotnet test tests/Barbatos.Wpf.Aquarius.UnitTests -f net8.0-windows --filter "FullyQualifiedName~KeepAliveTests"
```

Run a sample:

```bash
dotnet run --project samples/Barbatos.Wpf.Core.Sample
```

CI (`.github/workflows/barbatos-wpf-cd-nuget.yml`) builds `Release` with
`-p:SourceLinkEnabled=true` and pushes `.nupkg` + `.snupkg`. `GeneratePackageOnBuild` is on
for every `src/` project, so a plain `dotnet build` already produces packages.

---

## 3. Non-negotiables

### Target frameworks

- **.NET 8, 9, 10 only.** Never add a `netstandard`, `net6`, or `net7` target, and never write
  a compatibility shim for one.
- Consumers of **Core** must use the SDK-suffixed TFM (`net10.0-windows10.0.17763.0`). The
  bare `net10.0-windows` form falls back to the `windows7.0` baseline, which has no WinRT
  projections, and toast notifications fail to restore (`NU1201`). Aquarius/Router/Valee have
  no such requirement.

### C# 14

`LangVersion` is `14.0` repo-wide, but the libraries compile against three runtimes. Split
language features by what they need:

- **Compiler-only — use freely on every TFM:** the `field` keyword, extension members
  (`extension(...)` blocks), `nameof` over unbound generics, null-conditional assignment
  (`obj?.Prop = value`), collection expressions, primary constructors, `required` members,
  file-scoped namespaces, list patterns, `partial` properties.
- **Runtime-dependent — must be guarded:** `System.Threading.Lock` (.NET 9+),
  `params ReadOnlySpan<T>` overloads that only exist in newer BCLs, and anything else whose
  *type* ships with the runtime rather than the compiler. Guard with
  `#if NET9_0_OR_GREATER` / `#if NET10_0_OR_GREATER`, or don't use it.

Already idiomatic here and preferred: collection expressions (`= []`, `[.. _recency]`),
primary constructors for small internal types, `required` init properties for
configuration-shaped records (`RouteRecord`, `RouteLocation`), pattern matching over
`as`+null-check.

### Dependency injection is the design constraint, not a detail

Every feature must be reachable through `Microsoft.Extensions.DependencyInjection`:

- New Core feature ⇒ a `ConfigureXxx(this WpfAppBuilder, Action<XxxOptions>? = null)` method
  that binds `Barbatos:Xxx` from configuration (file values override code values) and
  registers its runtime service.
- New Aquarius/Router/Valee surface ⇒ `AddXxx(this IServiceCollection)` declares only, and
  `UseXxx(this IServiceProvider)` performs the one-shot wiring after `Build()`. This two-call
  split exists because XAML-driven resolution (`aq:Setup.Enable`, `Component.Is`,
  `RouterView`) has no constructor to inject into and needs an ambient provider.
- Use `TryAdd*` for anything an app might want to replace (`IAiApiKeyProvider`,
  `IMcpTokenStore`, `IPushNotificationTransport`, `IRouterHistory`, …), so a registration
  made *before* the `Configure`/`Add` call wins.
- Never build or own a container inside Aquarius/Router/Valee — `Abstractions` only.
- Anything an app constructs on Barbatos' behalf goes through `Setup.Resolve` (container
  first, `Activator.CreateInstance` fallback). Do not add a second construction path.

### No `InternalsVisibleTo`

This repo does not use it. If a test needs to reach internal behavior, that behavior must
become real, documented, supported **public** API — or the test must be rewritten against the
public surface. A testing-only seam is not an acceptable outcome.

### Public API discipline

- XML docs on every public type and member. `<remarks>` carries the *why*; this codebase's
  comments explain decisions, not mechanics.
- Prefer `sealed`. Exception: `Apsu` stores must be non-`sealed` with `virtual` actions for
  Castle DynamicProxy to intercept them.
- Configuration/descriptor types get a pure `Validate()` that throws
  `InvalidOperationException` without doing I/O — the pre-flight a settings UI calls before
  saving (`PeriodicSchedule`, `McpServerDescriptor`, `McpOAuthOptions`,
  `AiProviderDescriptor`, `AiAgentDescriptor`).
- Renaming a public member means keeping the old one as `[Obsolete]` (see
  `IAppInfo.AppGuid` → `AppId`), not deleting it.
- New trim/AOT `NoWarn` entries in a `.csproj` must carry a comment naming the exact
  reflection call and why it is inherently dynamic. Follow the existing blocks.

### Code style (`.editorconfig` is the authority)

- MIT file header on every `.cs` file in `src/` and `tests/` — the exact
  `file_header_template` text.
- File-scoped namespaces; `_camelCase` private fields; `PascalCase` everything public;
  `Async` suffix on async methods; interfaces prefixed `I`.
- Braces on their own line; no multiple blank lines; no blank line after `{`.
- `Nullable` and `ImplicitUsings` are enabled everywhere — do not re-enable them per project
  and do not disable them locally.

### Package versions

Central Package Management: **all** versions live in `Directory.Packages.props`, never in a
`.csproj`. `Microsoft.Extensions.AI*` is pinned to the floor `ModelContextProtocol.Core 2.2.0`
depends on — bumping one without the other produces an `NU1605` downgrade.

---

## 4. Documentation is part of the change

Every package ships two documents, both packed into the `.nupkg`:

- `src/<Package>/README.md` — concept-first walkthrough, worked examples, rationale.
- `src/<Package>/API-REFERENCE.md` — member-by-member signatures and exact behavior.

**A public API change is not done until both are updated**, plus the root `README.md` if the
package's one-paragraph summary changed. See the `barbatos-docs-sync` skill.

---

## 5. Skills

Task-specific playbooks live in [`skills/`](skills/). Load the one matching the work.

| Skill | Use it when |
| --- | --- |
| [`barbatos-wpf-host`](skills/barbatos-wpf-host/SKILL.md) | Composing `WpfApp`, DI/config/logging, lifecycle events, tray/startup/single-instance/notifications/periodic/input/dialogs/splash |
| [`barbatos-aquarius-view`](skills/barbatos-aquarius-view/SKILL.md) | Any View/ViewModel using `aq:` — reactivity, lifecycle hooks, directives, Teleport, Transition, Suspense, Slots, KeepAlive |
| [`barbatos-aquarius-router`](skills/barbatos-aquarius-router/SKILL.md) | Route tables, guards, `RouterView`/`RouterLink`, nested/named/modal routes, in-app search |
| [`barbatos-valee-form`](skills/barbatos-valee-form/SKILL.md) | Forms and validation — `Form`/`Field<T>`, DataAnnotations, cross-field rules, `aqv:` markup |
| [`barbatos-apsu-store`](skills/barbatos-apsu-store/SKILL.md) | State management — `StoreBase`, `Getter<T>`, `[Action]` interceptors, plugins |
| [`barbatos-mcp-ai`](skills/barbatos-mcp-ai/SKILL.md) | MCP servers, BYOK chat, agents, tool approval, model catalog, OAuth |
| [`barbatos-package-authoring`](skills/barbatos-package-authoring/SKILL.md) | Adding or changing public API in `src/`, csproj/build/packaging changes |
| [`barbatos-wpf-testing`](skills/barbatos-wpf-testing/SKILL.md) | Writing or running tests — STA threading, dispatcher pumping, GC leak probes |
| [`barbatos-docs-sync`](skills/barbatos-docs-sync/SKILL.md) | Updating `README.md` / `API-REFERENCE.md` after an API change |

### Making them slash-invokable (optional, once per clone)

Claude Code discovers skills under `.claude/skills/`. The canonical copies live in `skills/`;
link them once so `/barbatos-…` works. A directory junction needs no elevation, unlike a
symlink. From the repo root, in PowerShell:

```powershell
New-Item -ItemType Junction -Path .claude\skills -Target skills
```

…or from Git Bash (the doubled slashes stop MSYS from rewriting the flags as paths):

```bash
cmd //c mklink //J ".claude\skills" "skills"
```

`.gitignore` already excludes `.claude/skills/`, so the junction stays out of git and the one
source of truth remains `skills/`. Without the junction the skills are still fully usable —
the table above gives each path, and [`skills/README.md`](skills/README.md) summarizes them.
