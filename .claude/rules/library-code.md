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
2. **Which package owns it?** `Barbatos.Wpf.Aquarius` stays Core-free; its only packages are
   `CommunityToolkit.Mvvm`, `Microsoft.Extensions.DependencyInjection.Abstractions` and `Options`.
   Router and Valee may use Aquarius. No new `src/` → `src/` reference or new package dependency
   for these three without asking the maintainer.
3. **Is it reachable through DI?** If not, redesign it until it is. The exception is plain state
   the app constructs itself — Valee's `Form` / `Field<T>` are built by their ViewModel and need
   no registration.

## Language features across net8 / net9 / net10

`LangVersion` is 14.0 everywhere, but every library compiles for three runtimes.

- Compiler-only features are fine on every TFM: `field`, `extension(...)` blocks, `nameof` over
  unbound generics, `obj?.Prop = value`, collection expressions, primary constructors,
  `required`, list patterns, `partial` properties.
- Anything whose *type* ships with the runtime must be guarded with `#if NET9_0_OR_GREATER` /
  `#if NET10_0_OR_GREATER` or not used — `System.Threading.Lock` is the usual trap.

## DI entry points

- Core feature → `ConfigureXxx(this WpfAppBuilder builder, Action<XxxOptions>? configure = null)`
  returning the builder, binding `XxxOptions.SectionName` (`Barbatos:Xxx`) after the code delegate
  so file values override code values. A feature with several option types takes one optional
  delegate per type (`ConfigureMcp`). Apsu is the deliberate exception: stores register per type
  with `AddStore<TStore>` / `AddStoreInterceptor` on `IServiceCollection`.
- Aquarius and Router → `AddAquarius` / `AddAquariusRouter(this IServiceCollection, …)` only
  declare; `UseAquarius` / `UseAquariusRouter(this IServiceProvider)` do the one-shot wiring after
  `Build()` (`UseAquarius` returns the provider, `UseAquariusRouter` the `Router`). XAML-driven
  resolution has no constructor to inject into, so it needs that ambient provider. Valee has no
  such pair today; a new Valee surface that needs services follows the same split. Never build or
  own a container inside these three packages.
- Register replaceable services with `TryAdd*` so an app's earlier registration wins.
- In Aquarius / Router / Valee, anything constructed on the app's behalf (Views, ViewModels, routed
  windows) goes through `Setup.Resolve` (container first, `Activator.CreateInstance` fallback) —
  never a second construction path. Core cannot see `Setup`: it resolves from the container
  (`GetRequiredService`, or `ActivatorUtilities.CreateInstance` as `AddStore` does).
- Put OS interop behind a platform interface (`ITrayIconPlatform`, `INotificationPlatform`,
  `IHotkeyPlatform`, `IStartupRegistrar`, `IPowerManager`) so the service is testable headless.

## Public API discipline

- XML docs on every public type and member; `<remarks>` records the *why* and what was verified
  empirically, never what the code obviously does.
- New types are `sealed`. Apsu stores are the exception: non-`sealed` with `virtual` `[Action]`
  methods, because Castle DynamicProxy must subclass them. Many shipped types (Core `*Options`,
  the MCP descriptors, several Aquarius controls) are unsealed — never seal one later, that is a
  breaking change.
- Descriptor and schedule types that a settings UI builds from user input get a pure
  `Validate()` that throws `InvalidOperationException` and does no I/O — the pre-flight before
  saving (`PeriodicSchedule`, `McpServerDescriptor`, `McpOAuthOptions`, `AiProviderDescriptor`,
  `AiAgentDescriptor`). Plain `*Options` classes bound from configuration do not carry one.
- Rename = keep the old member as `[Obsolete]` returning the same value (`IAppInfo.AppGuid` →
  `AppId`). Never delete public API outside a deliberate major release.
- A **new** attached property that only works on certain element types throws
  `InvalidOperationException` on anything else instead of silently doing nothing (`Valee.Mask`,
  `Setup`). A few shipped handlers (`Lifecycle.Enable`) return silently; making one throw is a
  breaking change, so ask first.
- A new CLR namespace holding XAML types in Aquarius / Router / Valee gets its own
  `XmlnsDefinition` line in that package's `AssemblyInfo.cs`, so one prefix (`aq`/`aqr`/`aqv`)
  keeps spanning the whole package.
- **Never add `InternalsVisibleTo`.** If a test needs internal behaviour, it becomes real,
  documented public API, or the test uses the public surface.

## Controls, attached properties and directives (Aquarius / Router / Valee)

- WPF raises `Loaded` a second time with no `Unloaded` in between (a `TabControl` realizes every
  tab, then raises `Loaded` again on selection). Subscribe only on the transition into the loaded
  state (a `_loaded` flag, as `Teleport.OnLoaded` does), unsubscribe on `Unloaded`, and re-hook
  whatever you unhook. A handler left on a `static` event or an app-lifetime object pins the
  control, its tree and its DataContext for the life of the process.
- Such a control ships with a `GcProbe.AssertCollected` test paired with an `AssertAlive` control
  (`LifecycleLeakTests.cs`), plus a `XamlReader.Parse` test through the package xmlns
  (`ExprXamlTests.cs`, `SlotXamlTests.cs`) — see `barbatos-wpf-testing`.

## Done means

All three TFMs build with no new warnings; tests run per TFM (`barbatos-wpf-testing`); the
package's `README.md` **and** `API-REFERENCE.md` are updated, plus the root `API-REFERENCE.md` for
a new or removed namespace (`barbatos-docs-sync`); the sample
demonstrates the feature; a template under `templates/` is updated if the scaffolded shape
changed (`barbatos-templates`); and every `.claude/skills/**` line that names the changed member
or describes its changed behaviour is corrected in the same change
(`grep -rn "<Member>" .claude/skills`).
