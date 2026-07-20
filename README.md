# Barbatos.Wpf

![Barbatos.Wpf logo](https://github.com/Barbatos-Labs/Barbatos.Wpf/blob/main/build/nuget.png?raw=true)

### *A family of libraries that bring proven application-development ergonomics to plain desktop WPF*

[![GitHub stars](https://img.shields.io/github/stars/Barbatos-Labs/Barbatos.Wpf?style=social)](https://github.com/Barbatos-Labs/Barbatos.Wpf/stargazers)
[![License](https://img.shields.io/github/license/Barbatos-Labs/Barbatos.Wpf)](https://github.com/Barbatos-Labs/Barbatos.Wpf/tree/main/LICENSE.md)

Barbatos.Wpf ports well-established ideas from other ecosystems onto plain, unpackaged
desktop WPF - keeping each source ecosystem's own shape and naming wherever that maps
honestly, and clearly documenting it where WPF genuinely differs. It ships as several
independent NuGet packages rather than one monolith; install only what you need.

## Packages

| Package | Ports | Docs |
| --- | --- | --- |
| **Barbatos.Wpf.Core** | .NET MAUI's `MauiApp`/`MauiAppBuilder` hosting model and Essentials APIs (`AppInfo`, `Preferences`, `SecureStorage`, `Connectivity`, ...) - dependency injection, configuration, logging, and lifecycle events for WPF. | [README](src/Barbatos.Wpf.Core/README.md) · [API Reference](src/Barbatos.Wpf.Core/API-REFERENCE.md) |
| **Barbatos.Wpf.Aquarius** | Vue 3's Composition API - `ref`/`computed`/`watch`, lifecycle hooks, `v-model`/`v-show`/`v-if`/`v-on`, custom directives, `<Teleport>`, `<Transition>`, `provide`/`inject`, `<Suspense>` - built directly on CommunityToolkit.Mvvm. | [README](src/Barbatos.Wpf.Aquarius/README.md) · [API Reference](src/Barbatos.Wpf.Aquarius/API-REFERENCE.md) |

Both packages share the same `Barbatos.Wpf` root C# namespace and target
`net8.0-windows`/`net9.0-windows`/`net10.0-windows`; Aquarius has no dependency on Core
and works standalone.

```powershell
dotnet add package Barbatos.Wpf.Core
dotnet add package Barbatos.Wpf.Aquarius
```

## Repository layout

- `src/` - the libraries, one folder per package.
- `samples/` - one complete sample application per package.
- `tests/` - one unit test project per package.

See each package's own README (linked above) for everything else: quick start, full
feature walkthroughs, and a link to its detailed API reference.

---

## Community

### Maintainers

- Pham The Hung ([@StHung](https://github.com/StHung))

### Support

For support, please open a [GitHub issue](https://github.com/Barbatos-Labs/Barbatos.Wpf/issues/new). We welcome bug reports, feature requests, and questions.

### License

This project is licensed under the terms of the **MIT** open source license. Please refer to the [LICENSE](https://github.com/Barbatos-Labs/Barbatos.Wpf/blob/main/LICENSE.md) file for the full terms.

You can use it in private and commercial projects. Keep in mind that you must include a copy of the license in your project.
