# Skill triggering evals

Measures whether the repo's nine `barbatos-*` skills load for the right prompts — and, just as
importantly, stay out of the way for the wrong ones.

All nine cover one domain, so the interesting failure is not "did a skill fire" but **which
one**. A router question that pulls in only `barbatos-aquarius-view`, or a plain
CommunityToolkit.Mvvm question that pulls in anything at all, is a description problem worth
catching before it wastes anyone's context.

## Running

Needs the `claude` CLI authenticated (`claude` → `/login`) and Node 18+. From the repo root:

```bash
node skills/.evals/run-trigger-evals.mjs
```

Useful flags: `--model <id>` (default `claude-opus-5`), `--concurrency N` (default 4),
`--only 3,7,19` to re-check a few, `--timeout <seconds>` (default 120).

Each query is run in this repo with all nine skills installed, so it measures the real
competition between them. The run is cut short as soon as the routing decision is observable —
the point is which skill Claude reaches for, not the quality of the answer.

Results land in `results/trigger-<timestamp>.json`, and are not committed.

Two Windows notes, both learned the hard way while building this:

- The runner spawns `claude` with `shell: true`, because `claude` is a `.cmd` shim that Node
  refuses to spawn directly (`ENOENT`). Without it every query silently scores `NO-TRIGGER` —
  a whole run of fake failures that looks exactly like a real result.
- Node prints a `DEP0190` warning about `shell: true` with args. It does not apply here: the
  query goes through stdin and every command-line argument is a fixed literal.

If the CLI is not logged in, the script prints `AUTH-ERROR` and exits non-zero rather than
reporting no-triggers, because an unauthenticated reply is indistinguishable from a genuine
"answered without a skill".

## Verdicts

| Verdict | Meaning |
| --- | --- |
| `PASS` | Exactly the expected skills fired (or nothing fired, when nothing should) |
| `PASS+extra` | Every expected skill fired, plus another. Usually fine — several skills legitimately apply to one screen — but check it isn't noise |
| `NO-TRIGGER` | A skill was expected and none fired. The most common defect: the description is written in library vocabulary while the user typed problem vocabulary |
| `WRONG-SKILL` | Something fired, but not what was expected. A disambiguation problem between two sibling descriptions |
| `FALSE-POSITIVE` | A skill fired on a query that should have been handled without one |
| `AUTH-ERROR` | The CLI could not authenticate — the numbers mean nothing, re-login and re-run |

## The query set

`trigger-evals.json` — 24 queries. 16 should trigger, covering all nine skills (one in
Vietnamese); 8 should not.

The negatives are deliberately near-misses that share vocabulary with a skill but need
something else: CommunityToolkit.Mvvm in a plain WPF app, DataAnnotations in ASP.NET Core,
`beforeRouteLeave` in an actual Vue project, generic xUnit, calling OpenAI from a console app.
An obviously-unrelated negative ("write a fibonacci function") tests nothing — keep new ones
genuinely tempting.

## When to re-run

After editing any skill's `description`, and after adding a skill — a new sibling changes the
competition for every existing one. Add queries alongside a new skill, both positive and a
near-miss negative aimed at whichever existing skill it most resembles.
