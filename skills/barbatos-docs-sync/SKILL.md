---
name: barbatos-docs-sync
description: Keep the Barbatos.Wpf documentation correct after an API change - the split between each package's README.md (concept-first walkthrough) and API-REFERENCE.md (member-by-member signatures), the Documentation Menu anchors, the root README and API-REFERENCE, the house voice, and the sample and template updates that go with a documented feature. Use this after adding, renaming, deprecating or removing any public API, when asked to document or write up a feature, when a README section, anchor or menu entry looks stale or missing, and as the closing step of any src/ change. Reach for it on "what else do I need to update before this ships", "anything left before I merge this", "did I forget anything" - in this repo a public API change is not finished until both documents are updated, so raise it even when the user only asked for the code. Vietnamese prompts land here too: cập nhật tài liệu, viết docs cho tính năng mới, còn thiếu gì trước khi ship, mục lục README bị lệch, quên cập nhật API reference.
---

# Documentation upkeep

Every package ships two documents, and **both are packed into the `.nupkg`** — `README.md` via
`PackageReadmeFile`, so it is the first thing a NuGet visitor reads. Docs are not an afterthought
here; they are ~430 KB and carry most of the reasoning behind the API.

**A public API change is not done until both documents are updated.**

---

## The split

| File | Answers | Register |
| --- | --- | --- |
| `src/<Package>/README.md` | "What is this and how do I use it?" | Concept-first prose, runnable examples, tables comparing options, an explicit rationale for every non-obvious decision |
| `src/<Package>/API-REFERENCE.md` | "What is the exact signature and behavior of this member?" | Terse member tables and signature blocks, modeled on the official .NET API docs |

Do not duplicate. The API reference links back into the README for the *why*
(`See [Navigation Guards](README.md#navigation-guards) for the pipeline order`); the README links
forward for exact signatures.

Root-level `README.md` (package lineup, install, templates) and `API-REFERENCE.md` (namespace
index per package) only change when a package's one-paragraph summary or namespace list changes.

---

## README structure — follow it exactly

Every package README has the same skeleton:

```
# Barbatos.Wpf.<Package>
### *One-line positioning statement*
badges

## 📖 Documentation Menu      <- an anchor list of every ## and ### below
## Getting Started
### Introduction              <- "What is X?", what it is NOT, prerequisites blockquote
### Quick Start               <- dotnet add package, the XAML namespace, minimal wiring
## <feature sections>
## Deliberately Out of Scope  <- what was considered and rejected, with reasons
## Ecosystem
### Repository layout
## API Reference
## Community                  <- Maintainers / Support / License
```

**The Documentation Menu must list every `##` and `###` section.** It drifts easily: adding a
section without adding its anchor makes the new material effectively unfindable. Check the menu
against the actual headings whenever you touch a README.

Anchors are GitHub-style: lowercase, spaces to `-`, punctuation dropped —
`### Valee.Mask - guiding what gets typed` → `#valeemask---guiding-what-gets-typed`. Verify
anchors you write rather than guessing.

---

## API-REFERENCE structure

```
# Barbatos.Wpf.<Package> API Reference
one-paragraph purpose + a pointer to the README
## Namespaces          <- table: namespace -> contents, each linking to its own section
---
## Namespace: X        <- one section per namespace, types in dependency order
### `TypeName` (kind)  <- signature block, then a member table
```

Member tables carry `Member | Type/Signature | Meaning`. Record exact behavior: what throws and
with which exception type, what a `null` return means, what is cached versus recomputed, what
default a property has, which values are `internal`-set.

---

## House voice

The distinguishing feature of this codebase's prose is that it **records decisions and evidence**,
not mechanics. Match it:

- Say *why* a design was chosen, and what it was chosen over.
  > "Built on `Watch.On`, not `Watch.Effect` — `Watch.On` only reacts to the source's value
  > changing, which is what keeps two mutually-dependent fields from infinite-looping."
- State what was **verified empirically**, and say so.
  > "confirmed by actually running them"; "confirmed empirically"; "confirmed by testing".
- Name the limits honestly instead of implying completeness.
  > "Worth being explicit about the limits of this: FCM and WNS don't map onto a client-managed
  > persistent connection the way SignalR does."
- Where a Vue / vee-validate / Pinia / Vue Router concept was ported, say what it maps to and
  where the port deliberately differs.
- Use `> [!IMPORTANT]` / `> [!NOTE]` only for things that genuinely bite (a deadlock rule, a TFM
  requirement).
- Keep "Already native" notes: when WPF already does something, the doc says so rather than the
  library growing an API.
- Prose wraps at roughly 95 characters. Keep it.

Avoid: marketing adjectives, "simply"/"just", and describing what the code obviously does.

---

## The checklist for one API change

1. **`API-REFERENCE.md`** — add or amend the member row/section. Exact signature, exceptions,
   defaults.
2. **`README.md`** — the feature section, with a worked example and the rationale.
3. **Documentation Menu** — add the anchor for any new `##`/`###`.
4. **Cross-links** — README → API reference and back; sibling packages if the feature spans them
   (e.g. a Router feature that relies on Aquarius `Setup`).
5. **Root `README.md`** — only if the package summary changed.
6. **Root `API-REFERENCE.md`** — only if a namespace was added or removed.
7. **The sample** — this repo proves an API is usable by demonstrating it. Add or extend a tab /
   route / field, with a comment explaining what it demonstrates.
8. **`templates/`** — update the scaffolded files and `templates/README.md` if the generated shape
   changed.

For a **removal**, keep the member documented as `[Obsolete]` with what replaces it, the way
`IAppInfo.AppGuid` → `AppId` is documented — do not silently delete the section.

---

## Known drift to fix when you are next in the file

- `src/Barbatos.Wpf.AquariusValee/README.md`'s Documentation Menu is out of date — these `###`
  headings exist in the body but have no menu entry: `FieldMeta`, `FormMeta`, "Focusing the first
  invalid field on submit", "The schema-mode equivalent: `PropertyRangeAttribute` and
  `IDependsOnProperties`", `Valee.Mask`, `Valee.Numeric`, `FieldTemplateSelector`,
  `SafeMessageConverter`, `PatternAttribute`, and "A full review of
  `System.ComponentModel.DataAnnotations`".
