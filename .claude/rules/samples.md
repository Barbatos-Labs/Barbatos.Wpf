---
paths:
  - "samples/**"
---

# Sample apps under `samples/`

The samples are how this repo proves an API is usable: a documented feature is demonstrated in
its package's sample. Load the feature skill for the API itself (`barbatos-wpf-host`,
`barbatos-wpf-essentials`, `barbatos-aquarius-view`, `barbatos-aquarius-router`,
`barbatos-valee-form`, `barbatos-apsu-store`, `barbatos-mcp-ai`).

- **One sample per package**, each demonstrating that package in isolation:
  `Barbatos.Wpf.Core.Sample`, `Barbatos.Wpf.Aquarius.Sample`, `Barbatos.Wpf.AquariusRouter.Sample`,
  `Barbatos.Wpf.AquariusValee.Sample`. Add a tab, route or field to the existing sample rather
  than creating a new app.
- **Never reference one sample app from another.** Shared views live in
  `Barbatos.Wpf.Samples.Shared` (a plain library), which the samples reference instead — its
  `.csproj` explains why.
- Samples consume the libraries through `ProjectReference` to `src/`, so a sample shows the
  current API, not a published one.
- A sample models a real consumer app: it sets its own `Product` / `Company` / `Version` /
  `AssemblyVersion` / `FileVersion` (otherwise it reports the library's values through `AppInfo`
  and to File Explorer), uses the SDK-suffixed TFM `net10.0-windows10.0.17763.0`, and enables
  `Nullable` / `ImplicitUsings` itself.
- Each demonstration carries a comment saying **what it demonstrates and why**, in the same voice
  as the library's XML docs.
- Run one with `dotnet run --project samples/<Sample>` (Windows only).
