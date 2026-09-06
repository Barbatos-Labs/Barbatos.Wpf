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

**What counts as a trigger.** Not "the very first tool call was Skill". Claude routinely runs a
grep or an `ls` to see where it is before deciding anything, and a skill loaded on the second
step still shapes the entire answer — scoring that as a miss would send you off rewriting a
description that was working. So a query is given a small budget of orienting tool calls
(`ORIENTING_TOOL_BUDGET`, currently 3) before it is settled as a no-trigger. `first_tool` and
`tools_used` are recorded either way, so the stricter reading is still available in the results.

**These are real agentic sessions in your working tree.** `Edit`, `Write` and `NotebookEdit`
are disallowed, but that is not airtight: `Bash` stays enabled (orienting greps are part of the
behaviour being measured) and a session can still change files through it. One run here did
exactly that — a Vietnamese query asking for a tray-icon shortcut went ahead and implemented
the feature in `Barbatos.Wpf.Core.Sample`, which then had to be reverted.

So the script snapshots `git status --porcelain` before and after, and prints a loud warning
listing anything that changed. **Commit or stash before a run**, and revert what the warning
names — none of it is work you asked for.

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

**`AUTH-ERROR` on the tail of a run usually means a usage limit, not a broken login.** Across
several runs here the failures clustered on the last few queries while everything before them
measured fine — the account hit its limit partway through. Those queries were not measured at
all; they are excluded from the pass rate, and re-running just those ids once the limit resets
is enough. Lower `--concurrency` if it keeps happening.

Read the script's own summary rather than re-scoring the JSON by hand. `actual: []` on an
auth-errored query looks exactly like a correct no-trigger, so any ad-hoc scoring that forgets
to check `auth_error` will silently count six dead queries as six passing negatives.

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

## What the measurements changed

Three findings from building this suite, each of which had already produced a wrong conclusion
before it was caught:

- **A budget of 3 orienting calls was too tight.** A query about unsaved-changes guards scored
  `NO-TRIGGER` at 3 and `PASS` at 10 — the description was never the problem. Default is now 6.
- **Descriptions were written in library vocabulary, users type problem vocabulary.** Adding the
  symptom wording a person actually uses ("the edit is silently lost when I click another item")
  moved queries from miss to hit without touching any skill body.
- **A Vietnamese prompt would not trigger a skill that its English translation triggered.** Every
  description now carries a short Vietnamese trigger clause. The near-miss control (id 28, a
  plain CommunityToolkit.Mvvm question in Vietnamese) still correctly triggers nothing, which is
  what makes the addition safe rather than just louder.

## When to re-run

After editing any skill's `description`, and after adding a skill — a new sibling changes the
competition for every existing one. Add queries alongside a new skill, both positive and a
near-miss negative aimed at whichever existing skill it most resembles.
