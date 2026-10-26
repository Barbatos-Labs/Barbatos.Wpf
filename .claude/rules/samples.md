---
paths:
  - "samples/**"
---

# Sample apps under `samples/`

The samples are how this repo proves an API is usable: a documented feature is demonstrated in
its package's sample. Load the feature skill for the API itself (`barbatos-wpf-host`,
`barbatos-wpf-essentials`, `barbatos-aquarius-view`, `barbatos-aquarius-router`,
`barbatos-valee-form`, `barbatos-apsu-store`, `barbatos-mcp-ai`).

- **One sample per package**, each focused on its own package: `Barbatos.Wpf.Core.Sample`,
  `Barbatos.Wpf.Aquarius.Sample`, `Barbatos.Wpf.AquariusRouter.Sample`,
  `Barbatos.Wpf.AquariusValee.Sample`. Add a tab, route or field to the existing sample rather
  than creating a new app. A sample may also reference `Barbatos.Wpf.Core` for the `WpfApp` host
  (the Router and Valee samples do) or to show packages combined (the Aquarius sample's Apsu
  interop demo) — the *libraries* stay independent; the sample apps need not.
- **Never reference one sample app from another.** Shared views live in
  `Barbatos.Wpf.Samples.Shared` (a plain library referencing Aquarius and AquariusRouter), which
  the Aquarius and AquariusRouter samples reference instead — its `.csproj` explains why.
- Samples consume the libraries through `ProjectReference` to `src/`, so a sample shows the
  current API, not a published one.
- A sample models a real consumer app: it sets its own `Product` / `Company` / `Version` /
  `AssemblyVersion` / `FileVersion` (otherwise it reports the library's values through `AppInfo`
  and to File Explorer), uses the SDK-suffixed TFM `net10.0-windows10.0.17763.0`, enables
  `Nullable` / `ImplicitUsings` itself, and sets `<IsPackable>false</IsPackable>` —
  `Directory.Build.props` treats every project whose name lacks `Test` as a package, so a sample
  without it packs a `.nupkg` on every build.
- Each demonstration carries a comment saying **what it demonstrates and why**, in the same voice
  as the library's XML docs.
- **Adding a demo:**
  - *Aquarius:* an `XyzDemoView` / `XyzDemoViewModel` pair, a `TabItem` in `MainWindow.xaml`
    hosting `<local:XyzDemoView DataContext="{Binding XyzDemo}" />`, and a `MainViewModel.XyzDemo`
    property built in its constructor with a `m => Log("Xyz", m)` callback, like the existing demos.
  - *Router:* View + ViewModel under `Views/<Area>/`, a `RouteRecord` in `Routing/AppRoutes.cs`
    with a `Name` (required once it has `Search` entries — a search hit navigates by name), a
    `RouterLink` in `MainWindow.xaml`, and a DI registration only if the ViewModel has
    constructor dependencies (the `WpfProgram.cs` comment explains why).
  - *Valee:* every visible string is `{local:SafeStringLocalizer Text='<Area>.<Key>'}` with the key
    added to **both** `Locales/en-US.json` and `Locales/vi-VN.json` — a plain
    `{i18n:StringLocalizer}` with a missing key renders an empty string with no error.
- Run one with `dotnet run --project samples/<Sample>` (Windows only).
