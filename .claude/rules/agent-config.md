---
paths:
  - "CLAUDE.md"
  - ".claude/**"
---

# Maintaining the agent configuration

How the pieces load is described in [`.claude/README.md`](../README.md). When changing them:

- **`CLAUDE.md` loads into every session.** Keep it under ~200 lines and limited to what applies to
  every task. Anything that applies only to some files belongs in a path-scoped rule here; a
  procedure that applies only to some tasks belongs in a skill.
- **A rule needs a `paths:` list** of globs relative to the repo root, quoted. Without one it
  loads into every session like `CLAUDE.md`. Keep rules short and point to the skill that has the
  detail.
- **Skill frontmatter is `name` + `description` only, as strict YAML.** Write the description as
  one double-quoted line. `name` equals the folder name and keeps the `barbatos-` prefix.
- **The description decides whether the skill is ever loaded.** Budget 450–800 characters
  (never over 900): what it covers with its most distinctive API names first, then the words a
  user actually types — including symptoms, not just library vocabulary — then a short clause
  steering away from the nearest sibling, then a short Vietnamese trigger clause. The listing is
  shared by every installed skill and trimmed when it overflows, so the front of the text matters
  most.
- **Bodies stay under 500 lines**; detail moves into a reference file one level deep, linked from
  `SKILL.md` with a "read when …" line. Links are relative to the file (from
  `.claude/skills/<name>/SKILL.md` the repo root is `../../../`).
- **Every identifier in a skill must exist in `src/`.** Skills are instructions an agent will
  follow; a wrong member name ships broken code into a public library.
- Adding or renaming a skill: update the table in `CLAUDE.md`, the combinations in
  `.claude/README.md`, and add positive and near-miss negative queries to
  `.claude/evals/skill-triggers/trigger-evals.json`.
- **Check before committing:** `node .claude/evals/lint.mjs` (static checks), then — after any
  description change — `node .claude/evals/skill-triggers/run-trigger-evals.mjs` (see its README;
  commit or stash first, it runs real sessions in the working tree).
