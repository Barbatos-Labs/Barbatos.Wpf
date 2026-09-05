---
name: barbatos-package-authoring
description: Add, change, or remove public API in the Barbatos.Wpf src/ libraries, or touch build and packaging - csproj conventions, target frameworks, Central Package Management, C# 14 across net8/9/10, XmlnsDefinition XAML namespaces, trim/AOT warning suppressions, DI entry-point design, options and Validate patterns, obsoletion instead of deletion, and the no-InternalsVisibleTo rule. Use this whenever editing anything under src/, Directory.Build.props/targets or Directory.Packages.props, and whenever the question is about a new public type or member, a breaking change, deprecating something, bumping or adding a NuGet dependency, packaging, strong naming, or which target frameworks are supported. This skill is about changing the library itself - for consuming its features inside an app, use barbatos-wpf-host instead.
---

# Authoring a Barbatos.Wpf package

Public API is the product. A careless signature is a breaking change in someone else's app.

---

## Before writing anything

Answer these three, in order:

1. **Does WPF already do this?** This codebase has a documented "patterns that are already
   native" precedent — `{Binding}`, `ItemsControl`+`DataTemplate`, `ContentControl.Content`, the
   `INotifyDataErrorInfo` adorner, `Preview*` tunneling events, `LengthAttribute(n, n)`. If a
   native equivalent exists, the right answer is a README note, not new API.
2. **Which package does it belong to?** Aquarius must stay Core-free and
   `Abstractions`-only. Router and Valee may use Aquarius. Nothing may add a third `src/` → `src/`
   reference without asking the maintainer.
3. **Is it reachable through DI?** If not, redesign it until it is.

Do not port a concept literally-shaped-differently. When porting from Vue / vee-validate /
Pinia / Vue Router, **read that project's own docs and copy the usage shape**; an invented
"better" design gets rejected. Verify claims about WPF's native behavior **by running it**, not
by assuming — the `TabControl`/`Frame` keep-alive table exists because assumptions were wrong.

---

## Project files

`Directory.Build.props` already supplies `LangVersion 14.0`, `Nullable`, `ImplicitUsings`,
`AllowUnsafeBlocks`, versioning, license/icon packaging, SourceLink, and
`GeneratePackageOnBuild` for every non-test project. A `src/*.csproj` therefore carries only:

```xml
<PropertyGroup>
  <PackageId>Barbatos.Wpf.Xyz</PackageId>
  <TargetFrameworks>net10.0-windows;net9.0-windows;net8.0-windows</TargetFrameworks>
  <UseWPF>true</UseWPF>
  <CommonTags>dotnet;wpf;windows;desktop;...</CommonTags>   <!-- full override, not an append -->
  <Description>...</Description>
  <RootNamespace>Barbatos.Wpf.Xyz</RootNamespace>
  <PackageIconSourceFile>xyz-logo.png</PackageIconSourceFile>
  <NoWarn>$(NoWarn);IL2026;...</NoWarn>
</PropertyGroup>
```

- **`CommonTags` is overridden wholesale, never appended** for a non-Core package — the shared
  value bakes in Core-specific words that would mislead NuGet search.
- **Core uses the SDK-suffixed TFM** (`net10.0-windows10.0.17763.0`) because it needs WinRT
  projections for toast notifications. Aquarius/Router/Valee use the bare `-windows` form.
- **Every `NoWarn` addition needs a comment** naming the exact reflection call and why it is
  inherently dynamic. `IsTrimmable`/`EnableTrimAnalyzer`/`EnableAotAnalyzer` are on repo-wide;
  WPF is never trimmed, but the analyzer still runs, so an unexplained suppression hides real
  information. Follow the existing blocks verbatim in tone.
- **Package versions go in `Directory.Packages.props` only.** `Microsoft.Extensions.AI*` must
  stay in lockstep with `ModelContextProtocol.Core`'s floor or central pinning yields `NU1605`.
- `<Version>` in `Directory.Build.props` is the single source for all four packages.

---

## C# 14 across three runtimes

`LangVersion` is `14.0`, but the libraries compile against .NET 8, 9, and 10.

- **Compiler-only features work on every TFM** — the `field` keyword, extension members,
  `nameof` over unbound generics, null-conditional assignment, collection expressions, primary
  constructors, `required` members, file-scoped namespaces, list patterns, partial properties.
  Use them.
- **Runtime-dependent features must be guarded** — `System.Threading.Lock` (.NET 9+),
  newer-BCL-only overloads, any *type* that ships with the runtime. Use
  `#if NET9_0_OR_GREATER` / `#if NET10_0_OR_GREATER`, or don't use it.

Never add a `netstandard`/`net6`/`net7` target or a shim for one.

---

## The DI entry point

Every package's public surface must be reachable from `IServiceCollection`.

**Core** — one builder method per feature:

```csharp
public static WpfAppBuilder ConfigureXyz(this WpfAppBuilder builder, Action<XyzOptions>? configure = null)
```

Bind `XyzOptions.SectionName` (`"Barbatos:Xyz"`) so **file values override code values**,
register the runtime service with `TryAdd*`, and put OS interop behind an `IXyzPlatform`
interface so the service stays unit-testable without a real window
(`ITrayIconPlatform`, `INotificationPlatform`, `IHotkeyPlatform`, `IStartupRegistrar`,
`IPowerManager` are the precedents).

**Aquarius / Router / Valee** — the two-call split:

```csharp
services.AddAquarius(options => ...);   // declares registrations only; touches no static
provider.UseAquarius();                 // one-shot wiring, after Build()
```

The split exists because XAML-driven resolution (`aq:Setup.Enable`, `Component.Is`,
`RouterView`) has no constructor to inject into and needs an ambient provider. `UseXxx` returns
something chainable. `AddXxx` must be safe to omit; `UseXxx` must be safe to call without it.

Take a **factory** parameter whenever the registration needs something DI-resolved
(`AddAquariusRouter(sp => AppRoutes.Build(sp.GetRequiredService<AuthState>()))`).

---

## Type-design conventions to follow

| Convention | Where it comes from |
| --- | --- |
| `sealed` by default | everything except `StoreBase` derivatives, which the proxy needs `virtual` |
| Reference type when identity is the semantics | `RouteRecord` is deliberately not a `record` |
| `record` for resolved, compared-by-value data | `RouteLocation`, `PathToken`, `NavigationFailure` |
| `required` init properties for configuration shapes | `RouteRecord.Path`, `McpServerDescriptor.Name` |
| A pure `Validate()` that throws `InvalidOperationException` and does no I/O | `PeriodicSchedule`, `McpServerDescriptor`, `McpOAuthOptions`, `AiProviderDescriptor`, `AiAgentDescriptor` |
| `ArgumentException` for a duplicate name vs `InvalidOperationException` for an invalid one | `IPeriodicServiceScheduler.Register` / `IMcpServerRegistry.AddServerAsync` |
| Registration/mutation returns `IDisposable` or a handle for undo | `Router.BeforeEach`, `Watch.On`, `InterceptorHandle` |
| Fail loudly rather than silently no-op, but make strictness opt-in | `Setup.ThrowOnUnresolved`, `Expr.ThrowOnUnresolvedIdentifiers` — both default `false` |

**XAML types:** register the namespace in `AssemblyInfo.cs` so one prefix spans every CLR
namespace in the package:

```csharp
[assembly: XmlnsDefinition("http://schemas.barbatos.co/aquariusvalee/2026/xaml", "Barbatos.Wpf.AquariusValee.Validation")]
```

An attached property settable only on certain element types should **throw** on anything else
(`Directives.Model`, `Valee.Numeric`, `TeleportHost.RegisterHost`), never silently do nothing.

---

## Removing and renaming

Keep the old member, mark it `[Obsolete]`, and have it return the identical value — see
`IAppInfo.AppGuid` → `AppId`, where even the old assembly-metadata key still works as a
fallback. Deletion happens in a deliberate major release, not as a side effect.

---

## No `InternalsVisibleTo`

This repo does not use it, and adding it is not an option. If a test needs internal behavior,
that behavior becomes real, documented, supported **public** API — or the test is rewritten
against the public surface. Weigh that cost before making something internal in the first place.

---

## Style

- MIT file header (`file_header_template` in `.editorconfig`) on every `.cs` file in `src/` and
  `tests/`.
- File-scoped namespaces, `_camelCase` private fields, braces on their own line, no double blank
  lines, `Async` suffix on async methods.
- **XML docs on every public type and member**, with `<remarks>` carrying the *why*. Match the
  existing register: comments in this codebase explain decisions and record what was verified
  empirically, not what the code obviously does.
- Prefer `var` where the type is apparent from the right-hand side; explicit types elsewhere.

---

## Definition of done

1. Builds clean for **all three TFMs**, no new warnings.
2. Tests added and run **per TFM** (see `barbatos-wpf-testing`).
3. `src/<Package>/README.md` and `src/<Package>/API-REFERENCE.md` both updated
   (see `barbatos-docs-sync`).
4. Root `README.md` updated if the package's summary paragraph changed.
5. The relevant sample demonstrates it — that is how this repo proves an API is usable.
6. A template under `templates/` updated if the scaffolded shape changed.
