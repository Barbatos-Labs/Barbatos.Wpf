#!/usr/bin/env node
// Static checks for the agent configuration under .claude/ and the root CLAUDE.md - the
// cheap, deterministic half of keeping it healthy (skill-triggers/ is the behavioural half).
//
// Dependency-free on purpose, like the trigger runner: the dev machines here have Node (the
// Claude CLI ships on it) and may not have a real Python or a YAML package.
//
// Usage:  node .claude/evals/lint.mjs        (exit code 1 when any ERROR is reported)
//
// Why each check exists:
// - Frontmatter is held to strict YAML. Claude Code quietly re-parses a broken frontmatter, but
//   claude.ai uploads, the Skills API and other agents do not - every skill this repo had
//   before used an unquoted "too: ..." in its description, which is a YAML syntax error.
// - Descriptions are budgeted. The skill listing the model sees is shared by every installed
//   skill and trimmed when it overflows, so a long description costs its siblings their
//   triggering text, not just itself.
// - Rule globs must match something. A path-scoped rule whose glob matches no file never
//   loads, and nothing else reports that.

import { execSync } from "node:child_process";
import { existsSync, readFileSync, readdirSync } from "node:fs";
import { dirname, join, normalize, relative, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const repoRoot = resolve(here, "..", "..");
const claudeDir = resolve(repoRoot, ".claude");

const DESCRIPTION_BUDGET = 900; // repo budget; the Agent Skills spec's hard limit is 1024
const DESCRIPTION_LIMIT = 1024;
const SKILL_MAX_LINES = 500;
const CLAUDE_MD_MAX_LINES = 200;

let errors = 0;
let warnings = 0;
const report = (level, file, message) => {
  if (level === "ERROR") errors++;
  else warnings++;
  console.log(`${level.padEnd(7)} ${relative(repoRoot, file).replaceAll("\\", "/")}: ${message}`);
};

// Frontmatter: only the subset this repo writes - `key: value` lines and `key:` followed by a
// `  - "item"` list. A double-quoted value must be valid JSON (a strict subset of YAML's
// double-quoted scalar); a plain value must not contain the sequences that end a YAML plain
// scalar early.
function parseFrontmatter(file, text) {
  if (!text.startsWith("---\n") && !text.startsWith("---\r\n")) {
    report("ERROR", file, "frontmatter must start on the first line with ---");
    return null;
  }
  const end = text.indexOf("\n---", 3);
  if (end < 0) {
    report("ERROR", file, "frontmatter is not closed with ---");
    return null;
  }
  const data = {};
  let listKey = null;
  for (const raw of text.slice(text.indexOf("\n") + 1, end).split(/\r?\n/)) {
    if (!raw.trim()) continue;
    const item = raw.match(/^\s+-\s+(.*)$/);
    if (item && listKey) {
      data[listKey].push(scalar(file, listKey, item[1]));
      continue;
    }
    const pair = raw.match(/^([A-Za-z][\w-]*):(?:\s+(.*))?$/);
    if (!pair) {
      report("ERROR", file, `frontmatter line is not "key: value": ${raw}`);
      continue;
    }
    const [, key, value] = pair;
    if (value === undefined || value === "") {
      data[key] = [];
      listKey = key;
    } else {
      data[key] = scalar(file, key, value);
      listKey = null;
    }
  }
  return data;
}

function scalar(file, key, value) {
  if (value.startsWith('"')) {
    try {
      return JSON.parse(value);
    } catch {
      report("ERROR", file, `${key}: double-quoted value is not a valid quoted string`);
      return value;
    }
  }
  if (value.startsWith("'")) return value.replace(/^'|'$/g, "").replaceAll("''", "'");
  if (/: |\s#/.test(value) || /^[\[\]{}>|*&!%@`,?-]/.test(value)) {
    report("ERROR", file, `${key}: plain value contains YAML syntax (": ", " #" or a leading indicator) - quote it`);
  }
  return value;
}

// GitHub heading slugs: lowercase, keep letters/digits/space/-/_, spaces become hyphens,
// duplicates get -1, -2, ... Inline code and link markup are reduced to their text first.
function headingSlugs(file) {
  const slugs = new Set();
  const seen = new Map();
  let fence = false;
  for (const line of readFileSync(file, "utf8").split(/\r?\n/)) {
    if (line.trimStart().startsWith("```")) {
      fence = !fence;
      continue;
    }
    const m = !fence && line.match(/^#{1,6}\s+(.*?)\s*#*\s*$/);
    if (!m) continue;
    const text = m[1].replace(/`([^`]*)`/g, "$1").replace(/\[([^\]]*)\]\([^)]*\)/g, "$1");
    const slug = [...text.trim().toLowerCase()]
      .filter((ch) => /[\p{L}\p{N} _-]/u.test(ch))
      .join("")
      .replaceAll(" ", "-");
    const n = seen.get(slug) ?? 0;
    seen.set(slug, n + 1);
    slugs.add(n === 0 ? slug : `${slug}-${n}`);
  }
  return slugs;
}

function checkLinks(file, text) {
  const prose = text.replace(/```[\s\S]*?```/g, "").replace(/`[^`\n]*`/g, "");
  for (const [, target] of prose.matchAll(/\]\(([^)\s]+)\)/g)) {
    if (/^[a-z]+:/i.test(target)) continue;
    const [path, anchor] = target.split("#");
    const resolved = path ? normalize(join(dirname(file), decodeURI(path))) : file;
    if (!existsSync(resolved)) {
      report("ERROR", file, `broken link ${target}`);
    } else if (anchor && resolved.endsWith(".md") && !headingSlugs(resolved).has(anchor)) {
      report("ERROR", file, `link ${target} points at a heading that does not exist`);
    }
  }
}

const globToRegExp = (glob) =>
  new RegExp(
    "^" +
      glob
        .replace(/[.+^${}()|[\]\\]/g, "\\$&")
        .replace(/\*\*\//g, "\u0000")
        .replace(/\*\*/g, "\u0001")
        .replace(/\*/g, "[^/]*")
        .replace(/\?/g, "[^/]")
        .replaceAll("\u0000", "(?:.*/)?")
        .replaceAll("\u0001", ".*") +
      "$",
  );

const markdownUnder = (dir) =>
  readdirSync(dir, { withFileTypes: true }).flatMap((e) => {
    const p = join(dir, e.name);
    if (e.isDirectory()) return e.name === "results" || e.name === "worktrees" ? [] : markdownUnder(p);
    return e.name.endsWith(".md") ? [p] : [];
  });

// --- skills ---------------------------------------------------------------------------------
const skillsDir = join(claudeDir, "skills");
for (const entry of readdirSync(skillsDir, { withFileTypes: true })) {
  if (!entry.isDirectory()) continue;
  const skillFile = join(skillsDir, entry.name, "SKILL.md");
  if (!existsSync(skillFile)) {
    report("ERROR", join(skillsDir, entry.name), "skill folder has no SKILL.md");
    continue;
  }
  const text = readFileSync(skillFile, "utf8");
  const fm = parseFrontmatter(skillFile, text);
  if (fm) {
    const extra = Object.keys(fm).filter((k) => k !== "name" && k !== "description");
    if (extra.length) report("WARNING", skillFile, `frontmatter keys beyond name/description: ${extra.join(", ")}`);
    if (fm.name !== entry.name) report("ERROR", skillFile, `name "${fm.name}" must equal the folder name "${entry.name}"`);
    const d = typeof fm.description === "string" ? fm.description : "";
    if (!d) report("ERROR", skillFile, "description is missing");
    else if (d.length > DESCRIPTION_LIMIT) report("ERROR", skillFile, `description is ${d.length} chars (spec limit ${DESCRIPTION_LIMIT})`);
    else if (d.length > DESCRIPTION_BUDGET) report("WARNING", skillFile, `description is ${d.length} chars (repo budget ${DESCRIPTION_BUDGET})`);
  }
  const lines = text.split(/\r?\n/).length;
  if (lines > SKILL_MAX_LINES) report("WARNING", skillFile, `${lines} lines - move detail into a reference file (limit ${SKILL_MAX_LINES})`);
  for (const f of readdirSync(join(skillsDir, entry.name))) {
    if (f.endsWith(".md") && f !== "SKILL.md" && !text.includes(`](${f}`)) {
      report("WARNING", join(skillsDir, entry.name, f), "reference file is not linked from SKILL.md");
    }
  }
}

// --- rules ----------------------------------------------------------------------------------
const tracked = execSync("git ls-files", { cwd: repoRoot, encoding: "utf8" }).split("\n").filter(Boolean);
const rulesDir = join(claudeDir, "rules");
if (existsSync(rulesDir)) {
  for (const file of markdownUnder(rulesDir)) {
    const fm = parseFrontmatter(file, readFileSync(file, "utf8"));
    if (!fm) continue;
    if (!Array.isArray(fm.paths) || !fm.paths.length) {
      report("WARNING", file, "no paths: list - this rule loads into every session, like CLAUDE.md");
      continue;
    }
    for (const glob of fm.paths) {
      if (!tracked.some((p) => globToRegExp(glob).test(p))) report("ERROR", file, `paths glob "${glob}" matches no tracked file`);
    }
  }
}

// --- links everywhere, CLAUDE.md size ---------------------------------------------------------
const claudeMd = join(repoRoot, "CLAUDE.md");
for (const file of [claudeMd, ...markdownUnder(claudeDir)]) checkLinks(file, readFileSync(file, "utf8"));
const claudeLines = readFileSync(claudeMd, "utf8").split(/\r?\n/).length;
if (claudeLines > CLAUDE_MD_MAX_LINES) report("WARNING", claudeMd, `${claudeLines} lines - it loads into every session; move detail into a rule or skill`);

console.log(`\n${errors} error(s), ${warnings} warning(s)`);
process.exit(errors ? 1 : 0);
