# Barbatos.Wpf

![Barbatos.Wpf logo](https://github.com/Barbatos-Labs/Barbatos.Wpf/blob/main/build/nuget.png?raw=true)

### *Already know .NET MAUI or Vue? You already know Barbatos.Wpf.*

[![GitHub stars](https://img.shields.io/github/stars/Barbatos-Labs/Barbatos.Wpf?style=social)](https://github.com/Barbatos-Labs/Barbatos.Wpf/stargazers)
[![License](https://img.shields.io/github/license/Barbatos-Labs/Barbatos.Wpf)](https://github.com/Barbatos-Labs/Barbatos.Wpf/blob/main/LICENSE.md)

Barbatos.Wpf ports well-established ideas from other ecosystems onto plain, unpackaged
desktop WPF - no MAUI Blazor Hybrid, no JS runtime, no rewrite. Each package keeps its
source ecosystem's own shape and naming wherever that maps honestly, and clearly
documents it where WPF genuinely differs, rather than reinventing something WPF already
does natively. It ships as several independent NuGet packages rather than one monolith -
install only what you need, add the rest whenever you need it.

## Packages

<table>
<tr>
<td width="72" align="center">
<a href="https://github.com/Barbatos-Labs/Barbatos.Wpf/blob/main/src/Barbatos.Wpf.Core/README.md">
<img src="https://github.com/Barbatos-Labs/Barbatos.Wpf/blob/main/build/core-logo.png?raw=true" width="64" height="64" alt="Barbatos.Wpf.Core" />
</a>
</td>
<td>

**[Barbatos.Wpf.Core](https://github.com/Barbatos-Labs/Barbatos.Wpf/blob/main/src/Barbatos.Wpf.Core/README.md)**
&nbsp;
[![NuGet](https://img.shields.io/nuget/v/Barbatos.Wpf.Core.svg)](https://www.nuget.org/packages/Barbatos.Wpf.Core)
[![Downloads](https://img.shields.io/nuget/dt/Barbatos.Wpf.Core.svg)](https://www.nuget.org/packages/Barbatos.Wpf.Core)
[![Stars](https://img.shields.io/github/stars/Barbatos-Labs/Barbatos.Wpf?style=social)](https://github.com/Barbatos-Labs/Barbatos.Wpf/stargazers)

.NET MAUI's `MauiApp`/`MauiAppBuilder` hosting model and Essentials APIs (`AppInfo`,
`Preferences`, `SecureStorage`, `Connectivity`, ...) - dependency injection,
configuration, logging, and lifecycle events for WPF. Also ships an MCP (Model Context
Protocol) client + bring-your-own-key AI chat feature - your app's own end user supplies
their Gemini/Claude/ChatGPT/other API key, you never pay for their usage.

[README](https://github.com/Barbatos-Labs/Barbatos.Wpf/blob/main/src/Barbatos.Wpf.Core/README.md) · [API Reference](https://github.com/Barbatos-Labs/Barbatos.Wpf/blob/main/src/Barbatos.Wpf.Core/API-REFERENCE.md)

</td>
</tr>
<tr>
<td width="72" align="center">
<a href="https://github.com/Barbatos-Labs/Barbatos.Wpf/blob/main/src/Barbatos.Wpf.Aquarius/README.md">
<img src="https://github.com/Barbatos-Labs/Barbatos.Wpf/blob/main/build/aquarius-logo.png?raw=true" width="64" height="64" alt="Barbatos.Wpf.Aquarius" />
</a>
</td>
<td>

**[Barbatos.Wpf.Aquarius](https://github.com/Barbatos-Labs/Barbatos.Wpf/blob/main/src/Barbatos.Wpf.Aquarius/README.md)**
&nbsp;
[![NuGet](https://img.shields.io/nuget/v/Barbatos.Wpf.Aquarius.svg)](https://www.nuget.org/packages/Barbatos.Wpf.Aquarius)
[![Downloads](https://img.shields.io/nuget/dt/Barbatos.Wpf.Aquarius.svg)](https://www.nuget.org/packages/Barbatos.Wpf.Aquarius)
[![Stars](https://img.shields.io/github/stars/Barbatos-Labs/Barbatos.Wpf?style=social)](https://github.com/Barbatos-Labs/Barbatos.Wpf/stargazers)

Vue 3's Composition API - `ref`/`computed`/`watch`, lifecycle hooks,
`v-model`/`v-show`/`v-if`/`v-on`, custom directives, `<Teleport>`, `<Transition>`,
`provide`/`inject`, `<Suspense>` - built directly on CommunityToolkit.Mvvm. No dependency
on Core - install it standalone or alongside.

[README](https://github.com/Barbatos-Labs/Barbatos.Wpf/blob/main/src/Barbatos.Wpf.Aquarius/README.md) · [API Reference](https://github.com/Barbatos-Labs/Barbatos.Wpf/blob/main/src/Barbatos.Wpf.Aquarius/API-REFERENCE.md)

</td>
</tr>
</table>

Both packages share the same `Barbatos.Wpf` root C# namespace and target
`net8.0-windows`/`net9.0-windows`/`net10.0-windows`.

```powershell
dotnet add package Barbatos.Wpf.Core
dotnet add package Barbatos.Wpf.Aquarius
```

## Templates

Scaffold new code instead of typing every `DataContext`/`PackageReference` line by hand -
`dotnet new` templates for both packages live in [`templates/`](https://github.com/Barbatos-Labs/Barbatos.Wpf/blob/main/templates/README.md).

Install once, from the repo root:

```powershell
dotnet new install ./templates/Barbatos.Wpf.Aquarius/item-templates/aquarius-view
dotnet new install ./templates/Combined/project-templates/barbatos-wpf-app
```

Then use from the CLI - this works identically inside Visual Studio 2022's or JetBrains
Rider's own integrated terminal, or a plain terminal:

```powershell
# A whole new starter app, referencing Barbatos.Wpf.Core, Barbatos.Wpf.Aquarius, and Barbatos.i18n:
dotnet new barbatos-wpf-app -n MyApp

# A View + ViewModel pair inside an existing project:
dotnet new aq-view -n Dashboard --namespace MyApp.Features.Dashboard
```

Both Visual Studio 2022 and Rider also index installed `dotnet new` templates into their
own GUI dialogs:
- **Visual Studio 2022**: the project template shows up under **File > New > Project** -
  search for "Barbatos.Wpf" or its short name, `barbatos-wpf-app`. The item template
  should appear under **Add > New Item** (right-click a project in Solution Explorer) the
  same way - search for "Aquarius" or `aq-view`.
- **Rider**: **File > New Solution** lists installed project templates alongside Rider's
  own; an installed item template is reachable via right-clicking a folder and choosing
  **New**, which lists templates by name.

GUI discovery can vary by IDE version - if a template doesn't show up in either picker, the
CLI form above always works, including from each IDE's own integrated terminal.

See [`templates/README.md`](https://github.com/Barbatos-Labs/Barbatos.Wpf/blob/main/templates/README.md) for the full list, folder layout, and
per-template details (including why `--namespace` has to be passed explicitly for the item
template, and why the project template uses `PackageReference` rather than a path into this
repo even though neither package is published to NuGet.org yet).

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
