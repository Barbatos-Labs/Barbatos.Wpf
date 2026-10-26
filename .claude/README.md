# Agent configuration for Barbatos.Wpf

Everything an AI coding agent needs to work in this repository, organized so that Claude Code
loads it **without being told to**: the always-on rules in [`CLAUDE.md`](../CLAUDE.md), the
file-specific rules in [`rules/`](rules/), and the task playbooks in [`skills/`](skills/).

```
CLAUDE.md                  always loaded - what the repo is, build/test, non-negotiables, skill index
.claude/
  README.md                this file
  rules/                   attached automatically when an agent opens a matching file (Read)
  skills/barbatos-*/       loaded automatically when a task matches the skill's description
  evals/
    lint.mjs               static checks for all of the above
    skill-triggers/        measures which skill actually fires for realistic prompts
  settings.local.json      personal, git-ignored (never committed)
```

## How each piece is loaded

| Piece | When it enters an agent's context | Holds |
| --- | --- | --- |
| `CLAUDE.md` | At the start of every session | Facts every task needs: the package map, how to build and test, the rules nobody may break |
| `rules/*.md` | When the agent opens a file matching the rule's `paths:` globs with the Read tool (so always before an Edit of it). Reading through Bash or creating a file with Write does not reliably trigger it | The conventions for that kind of file, short, pointing to the skill with the detail |
| `skills/*/SKILL.md` | Name and description are always listed; the body loads when the agent (or `/barbatos-…`) invokes it | A task playbook: how to do it, the decisions behind it, the failure modes |
| `skills/*/<reference>.md` | Only when the skill body points the agent at it | Long tables, full examples, rarely needed detail |

## Rules

| Rule | Loads for | Point it makes |
| --- | --- | --- |
| [`library-code.md`](rules/library-code.md) | `src/**/*.cs`, `src/**/*.xaml` | Public API discipline, DI entry points, C# 14 across net8/9/10, control `Loaded` guards and leak tests, no `InternalsVisibleTo`, definition of done |
| [`csharp-style.md`](rules/csharp-style.md) | `*.cs` in `src/`, `tests/`, `samples/`, `templates/`; `.editorconfig` | File header, naming, braces, nullable, `var`, comment voice, what never to change in `.editorconfig` |
| [`tests.md`](rules/tests.md) | `tests/**` | Per-TFM test runs, STA helpers, leak tests |
| [`docs.md`](rules/docs.md) | Package and root `README.md` / `API-REFERENCE.md`, `templates/README.md` | README vs API-REFERENCE split, menu anchors, house voice |
| [`build-and-packages.md`](rules/build-and-packages.md) | `*.csproj`, `Directory.*`, `nuget.config`, the solution, CI workflows | Central Package Management, TFMs, `NoWarn` comments, versioning, release pipeline |
| [`samples.md`](rules/samples.md) | `samples/**` | One sample per package, never sample-to-sample references, how to add a demo to each |
| [`templates.md`](rules/templates.md) | `templates/**` | Template layout, symbols, version coupling, test loop |
| [`agent-config.md`](rules/agent-config.md) | `CLAUDE.md`, `.claude/**` | How to change any of this safely |

## Skills

| Skill | Load it when |
| --- | --- |
| [`barbatos-wpf-host`](skills/barbatos-wpf-host/SKILL.md) | Composing a `WpfApp`, DI/configuration/lifecycle, and the opt-in desktop features — tray, single instance, run on startup, keep awake, notifications, push, periodic services, shortcuts and hotkeys, dialogs, splash screen |
| [`barbatos-wpf-essentials`](skills/barbatos-wpf-essentials/SKILL.md) | The Essentials platform services — app info and identity, preferences, secure storage, version tracking, connectivity, device/display info, email, launcher, jump list, permissions, licensing |
| [`barbatos-aquarius-view`](skills/barbatos-aquarius-view/SKILL.md) | Any View/ViewModel using `aq:` — reactivity, `Setup`, lifecycle hooks, directives, Teleport, Transition, Suspense, Slots, KeepAlive |
| [`barbatos-aquarius-router`](skills/barbatos-aquarius-router/SKILL.md) | Navigation — route tables, guards, `RouterView`/`RouterLink`, nested/named/modal routes, history, in-app search |
| [`barbatos-valee-form`](skills/barbatos-valee-form/SKILL.md) | Forms and validation — `Form`/`Field<T>`, DataAnnotations, cross-field and async rules, `aqv:` markup, masks, localized messages |
| [`barbatos-apsu-store`](skills/barbatos-apsu-store/SKILL.md) | Shared state — `StoreBase`, `Getter<T>`, `[Action]` interceptors, plugins |
| [`barbatos-mcp-ai`](skills/barbatos-mcp-ai/SKILL.md) | MCP servers, bring-your-own-key chat and agents, tool approval, providers/models, OAuth |
| [`barbatos-package-authoring`](skills/barbatos-package-authoring/SKILL.md) | Changing public API under `src/`, project files, dependencies, packaging and releases |
| [`barbatos-wpf-testing`](skills/barbatos-wpf-testing/SKILL.md) | Writing or running tests, and before trusting any test result |
| [`barbatos-docs-sync`](skills/barbatos-docs-sync/SKILL.md) | Updating `README.md` / `API-REFERENCE.md` after an API change, and "what else before this ships?" |
| [`barbatos-templates`](skills/barbatos-templates/SKILL.md) | The `dotnet new` templates — using, testing, changing or adding one |

Typical combinations:

- **New feature in `Barbatos.Wpf.Core`** → `barbatos-package-authoring` + `barbatos-wpf-host` (or
  `barbatos-wpf-essentials`) + `barbatos-wpf-testing` + `barbatos-docs-sync`.
- **New screen in an app** → `barbatos-aquarius-view`, plus `barbatos-aquarius-router` if it is
  routed and `barbatos-valee-form` if it has inputs.
- **Reviewing a PR that touches `src/`** → `barbatos-package-authoring` for the API rules,
  `barbatos-docs-sync` for the documentation obligation.
- **Releasing** → `barbatos-package-authoring` (version and pipeline) + `barbatos-templates`
  (the project template pins the package version).

## Keeping it healthy

```bash
node .claude/evals/lint.mjs                                # frontmatter, budgets, links, rule globs
node .claude/evals/skill-triggers/run-trigger-evals.mjs    # which skill fires for which prompt
```

Run the lint before every commit that touches this folder or `CLAUDE.md`, and the trigger evals
after changing a skill description, `CLAUDE.md` or a rule, or adding a skill (read
[`evals/skill-triggers/README.md`](evals/skill-triggers/README.md) first — they run real sessions
in your working tree). The conventions for editing all of this are in
[`rules/agent-config.md`](rules/agent-config.md).

## Upgrading a clone that used the old `skills/` junction

The skills used to live in a top-level `skills/` folder, reached through a hand-made
`.claude\skills` junction. If your clone still has that junction, remove it **before** pulling.
Use `cmd`'s `rmdir`, which deletes only the link — PowerShell's `Remove-Item` (and its `rmdir`
alias) can recurse into the junction's target on Windows PowerShell 5.1:

```powershell
cmd /c rmdir .claude\skills
git pull
```
