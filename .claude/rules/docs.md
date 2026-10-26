---
paths:
  - "README.md"
  - "API-REFERENCE.md"
  - "src/**/README.md"
  - "src/**/API-REFERENCE.md"
  - "templates/README.md"
---

# Package documentation

These files are part of the product: each package's `README.md` is packed into its `.nupkg`
and is the first thing a NuGet visitor reads; `API-REFERENCE.md` is read on GitHub (it is not
packed, so a link from a README to it must be an absolute GitHub URL, or it is dead on
nuget.org). Load `barbatos-docs-sync` for the full checklist, the README skeleton and the house
voice.

- **Split, don't duplicate.** `src/<Package>/README.md` is concept-first: what, how, worked
  examples, and the rationale for every non-obvious decision. `src/<Package>/API-REFERENCE.md` is
  member-by-member: exact signatures, defaults, what throws and with which exception type. Each
  links to the other instead of repeating it.
- **A public API change updates both files.** The root `README.md` changes only when a package's
  one-paragraph summary, the package list or the templates change; the root `API-REFERENCE.md`
  only when a namespace is added or removed.
- **The 📖 Documentation Menu lists every `##` and `###` heading.** Adding a section without its
  menu entry makes it unfindable. Anchors are GitHub slugs — lowercase, spaces → `-`,
  punctuation dropped — so verify each one against the heading you link to.
- **Voice:** record decisions and evidence, not mechanics. Say what a design was chosen over and
  why; say when something was verified by running it; name limits honestly; say what a Vue /
  vee-validate / Pinia / Vue Router concept maps to and where the port differs; keep "already
  native in WPF" notes instead of inventing API. No marketing adjectives, no "simply"/"just".
  `> [!IMPORTANT]` (the only callout the docs use; it renders on nuget.org too) only for things
  that genuinely bite.
- Wrap prose at roughly 95 characters, like the rest of the file.
- A removed or renamed member stays documented as `[Obsolete]` with its replacement.
- A documented feature also needs its sample (and template, if the scaffolded shape changed).
