---
paths:
  - "src/**/*.cs"
  - "src/**/*.xaml"
---

# Library code under `src/`

You are editing a published NuGet library. Public API is the product: a careless signature is a
breaking change in someone else's app. For the full playbook load `barbatos-package-authoring`;
for the feature's domain knowledge load the matching feature skill.

## Before adding anything

1. **Does WPF already do it?** If a native equivalent exists, the answer is a README note, not new
   API.
2. **Which package owns it?** `Barbatos.Wpf.Aquarius` stays Core-free and references only
   `Microsoft.Extensions.DependencyInjection.Abstractions` + `Options`. Router and Valee may use
   Aquarius. No new `src/` → `src/` reference without asking the maintainer.
3. **Is it reachable through DI?** If not, redesign it until it is.

## Language features across net8 / net9 / net10

`LangVersion` is 14.0 everywhere, but every library compiles for three runtimes.

- Compiler-only features are fine on every TFM: `field`, `extension(...)` blocks, `nameof` over
  unbound generics, `obj?.Prop = value`, collection expressions, primary constructors,
  `required`, list patterns, `partial` properties.
- Anything whose *type* ships with the runtime must be guarded with `#if NET9_0_OR_GREATER` /
  `#if NET10_0_OR_GREATER` or not used — `System.Threading.Lock` is the usual trap.

## DI entry points

- Core feature → `ConfigureXxx(this WpfAppBuilder builder, Action<XxxOptions>? configure = null)`,
  binding the `Barbatos:Xxx` section so file values override code values.
- Aquarius / Router / Valee → `AddXxx(this IServiceCollection)` only declares;
  `UseXxx(this IServiceProvider)` does the one-shot wiring after `Build()`. XAML-driven resolution
  has no constructor to inject into, so it needs that ambient provider. Never build or own a
  container inside these three packages.
- Register replaceable services with `TryAdd*` so an app's earlier registration wins.
- Anything constructed on the app's behalf goes through `Setup.Resolve` (container first,
  `Activator.CreateInstance` fallback). Never add a second construction path.
- Put OS interop behind a platform interface (`ITrayIconPlatform`, `INotificationPlatform`,
  `IHotkeyPlatform`, `IStartupRegistrar`, `IPowerManager`) so the service is testable headless.

## Public API discipline

- XML docs on every public type and member; `<remarks>` records the *why* and what was verified
  empirically, never what the code obviously does.
- `sealed` by default. Apsu stores are the exception: non-`sealed` with `virtual` `[Action]`
  methods, because Castle DynamicProxy must subclass them.
- Configuration and descriptor types get a pure `Validate()` that throws
  `InvalidOperationException` and does no I/O.
- Rename = keep the old member as `[Obsolete]` returning the same value (`IAppInfo.AppGuid` →
  `AppId`). Never delete public API outside a deliberate major release.
- An attached property that only works on certain element types throws on anything else instead
  of silently doing nothing.
- A new CLR namespace holding XAML types in Aquarius / Router / Valee gets its own
  `XmlnsDefinition` line in that package's `AssemblyInfo.cs`, so one prefix (`aq`/`aqr`/`aqv`)
  keeps spanning the whole package.
- **Never add `InternalsVisibleTo`.** If a test needs internal behaviour, it becomes real,
  documented public API, or the test uses the public surface.

## Done means

All three TFMs build with no new warnings; tests run per TFM (`barbatos-wpf-testing`); the
package's `README.md` **and** `API-REFERENCE.md` are updated (`barbatos-docs-sync`); the sample
demonstrates the feature; a template under `templates/` is updated if the scaffolded shape
changed (`barbatos-templates`).
