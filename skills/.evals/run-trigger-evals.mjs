#!/usr/bin/env node
// Measures which of the repo's barbatos-* skills Claude actually reaches for, given a
// realistic user query. All nine skills are installed together (see CLAUDE.md), so this
// records *which* skill fired rather than just whether one did - cross-triggering between
// sibling skills in one domain is the failure mode worth measuring here.
//
// Written in Node rather than Python only because this repo's dev machines have Node
// (the Claude CLI itself ships on it) and may not have a real Python.
//
// Usage:  node skills/.evals/run-trigger-evals.mjs [--model <id>] [--concurrency N] [--only 1,5,7]

import { spawn, execSync } from "node:child_process";
import { readFileSync, writeFileSync, mkdirSync, readdirSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const repoRoot = resolve(here, "..", "..");

const args = process.argv.slice(2);
const flag = (name, fallback) => {
  const i = args.indexOf(`--${name}`);
  return i >= 0 && args[i + 1] ? args[i + 1] : fallback;
};

const MODEL = flag("model", "claude-opus-5");
// Everything else on the command line is a fixed literal; this is the one operator-supplied
// value that reaches a shell, so keep it to the shape a model id actually has.
if (!/^[A-Za-z0-9._-]+$/.test(MODEL)) {
  console.error(`Refusing to run: --model "${MODEL}" is not a plain model id.`);
  process.exit(2);
}
const CONCURRENCY = Number(flag("concurrency", "4"));
const ONLY = flag("only", null)?.split(",").map(Number) ?? null;
const TIMEOUT_MS = Number(flag("timeout", "120")) * 1000;

// "The very first tool must be Skill" is too strict a bar for a repo skill set: Claude
// routinely runs a grep or two to see where it is before deciding, and a skill loaded on the
// third step still shapes the whole answer. Allow a few orienting calls, then settle - letting
// every query run to completion would be slow and would measure answer quality instead.
// The default was 3 and that was measurably too tight: a query about unsaved-changes guards
// scored NO-TRIGGER at 3 and PASS at 10, because Claude read it as a bug report and grepped
// the code first. Three descriptions were nearly rewritten over what turned out to be this
// number. When investigating a no-trigger, re-run it at a higher budget before touching any
// description - if it passes there, the description was never the problem.
const ORIENTING_TOOL_BUDGET = Number(flag("orienting-budget", "6"));

const suite = JSON.parse(readFileSync(resolve(here, "trigger-evals.json"), "utf8"));

// The skills that actually exist on disk, so a partially-streamed tool input can never be
// mistaken for one. Read from the directory rather than hardcoded, so adding a skill needs
// no edit here.
const KNOWN_SKILLS = readdirSync(resolve(here, ".."), { withFileTypes: true })
  .filter((e) => e.isDirectory() && e.name.startsWith("barbatos-"))
  .map((e) => e.name);
if (!KNOWN_SKILLS.length) {
  console.error("No barbatos-* skills found next to this script - nothing to measure.");
  process.exit(2);
}
const queries = ONLY ? suite.queries.filter((q) => ONLY.includes(q.id)) : suite.queries;

// Detect a skill invocation from the streaming events. We watch partial messages so a run
// can be cut short the moment the decision is observable, instead of paying for the whole
// task to finish - the decision is what we are measuring, not the answer quality.
function runQuery(q) {
  return new Promise((done) => {
    // shell:true is required on Windows, where `claude` is a .cmd shim that Node refuses to
    // spawn directly (ENOENT). Because shell:true concatenates args without escaping, the
    // query is fed through stdin instead of the command line - no quoting to get wrong, and
    // nothing user-supplied ever reaches the shell.
    const env = { ...process.env };
    delete env.CLAUDECODE;

    // These queries run as real agentic sessions in the working tree, so the tools that could
    // modify it are taken away. Bash stays: Claude routinely orients with a grep or ls before
    // deciding anything, and removing it would change the very behaviour being measured.
    const child = spawn(
      "claude",
      [
        "-p", "--model", MODEL,
        "--output-format", "stream-json", "--verbose", "--include-partial-messages",
        "--disallowedTools", "Edit", "Write", "NotebookEdit",
      ],
      { cwd: repoRoot, env, shell: true },
    );
    child.stdin.write(q.query);
    child.stdin.end();

    const skills = new Set();
    const toolsUsed = [];
    let firstTool = null;
    let pendingTool = null;
    let partial = "";
    let buf = "";
    let settled = false;
    let authError = false;
    let sawStreamEvents = false;

    const noteTool = (name) => {
      if (settled) return; // the decision is made; anything after it is not evidence
      firstTool ??= name;
      toolsUsed.push(name);
      if (name !== "Skill" && toolsUsed.filter((t) => t !== "Skill").length >= ORIENTING_TOOL_BUDGET) finish();
    };

    const finish = () => {
      if (settled) return;
      settled = true;
      clearTimeout(timer);
      try { child.kill(); } catch { /* already gone */ }
      done({
        id: q.id,
        query: q.query,
        expected: q.expected_skills,
        actual: [...skills],
        first_tool: firstTool,
        tools_used: toolsUsed,
        auth_error: authError,
      });
    };

    const timer = setTimeout(finish, TIMEOUT_MS);

    // Matched against the real skill names rather than a loose pattern: the tool input arrives
    // as a stream of JSON fragments, so a bare /barbatos-[a-z-]+/ also matches half-delivered
    // prefixes ("barbatos-val") and records them as extra skills that never fired.
    const noteSkill = (raw) => {
      for (const name of KNOWN_SKILLS) if (raw.includes(name)) skills.add(name);
    };

    const handleLine = (line) => {
      if (!line) return;

      // Check the raw line, not just the parsed `result` event: an auth failure also surfaces
      // as an ordinary assistant message, which would otherwise be scored as a legitimate
      // "answered without reaching for a skill" and report a whole run of false NO-TRIGGERs.
      if (/Failed to authenticate|Invalid API key|OAuth session expired/i.test(line)) authError = true;

      let ev;
      try { ev = JSON.parse(line); } catch { return; }

      {

        if (ev.type === "stream_event") {
          sawStreamEvents = true;
          const se = ev.event ?? {};
          if (se.type === "content_block_start" && se.content_block?.type === "tool_use") {
            pendingTool = se.content_block.name ?? null;
            partial = "";
            noteTool(pendingTool);
          } else if (se.type === "content_block_delta" && pendingTool === "Skill") {
            partial += se.delta?.partial_json ?? "";
            noteSkill(partial);
          } else if (se.type === "content_block_stop") {
            if (pendingTool === "Skill") {
              noteSkill(partial);
              finish(); // the routing question is answered the moment a skill loads
            }
            pendingTool = null;
          }
        } else if (ev.type === "assistant") {
          // Fallback for when partial-message events are unavailable: the same accounting,
          // just arriving a whole message at a time. Skipped when the stream already reported
          // this message's tools, or every call would be counted twice.
          if (sawStreamEvents) return;
          for (const c of (ev.message?.content ?? []).filter((c) => c.type === "tool_use")) {
            noteTool(c.name);
            if (c.name === "Skill") {
              noteSkill(JSON.stringify(c.input ?? {}));
              finish();
            }
          }
          // A tool-free reply is deliberately not settled here: it looks identical whether
          // Claude genuinely answered unaided or the CLI failed to authenticate, so wait for
          // `result`, which carries is_error.
        } else if (ev.type === "result") {
          if (ev.is_error) authError = true;
          finish();
        }
      }
    };

    child.stdout.on("data", (chunk) => {
      buf += chunk.toString("utf8");
      let nl;
      while ((nl = buf.indexOf("\n")) >= 0) {
        handleLine(buf.slice(0, nl).trim());
        buf = buf.slice(nl + 1);
      }
    });

    child.on("error", finish);
    // The final line usually arrives with no trailing newline, so it is still sitting in the
    // buffer at this point - and that line is the `result` event carrying is_error. Flushing
    // it before settling is what stops an auth failure being scored as a genuine no-trigger.
    child.on("close", () => {
      if (buf.trim()) handleLine(buf.trim());
      finish();
    });
  });
}

// Disallowing Edit/Write is not enough on its own: a session that wants to change a file can
// still do it through Bash, and one of these runs really did implement a whole feature in a
// sample that way. Bash has to stay enabled (orienting greps are part of the behaviour being
// measured), so the tree is snapshotted instead and any difference is reported loudly.
function treeState() {
  try {
    return execSync("git status --porcelain", { cwd: repoRoot, encoding: "utf8" });
  } catch {
    return null; // not a git repo, or git unavailable - skip the check rather than fail the run
  }
}

async function main() {
  const treeBefore = treeState();
  console.log(`Running ${queries.length} queries against ${MODEL} (concurrency ${CONCURRENCY})\n`);
  const results = [];
  const queue = [...queries];

  await Promise.all(
    Array.from({ length: Math.min(CONCURRENCY, queue.length) }, async () => {
      while (queue.length) {
        const q = queue.shift();
        const r = await runQuery(q);
        results.push(r);
        // expected_skills is the set of *acceptable* answers, not a checklist that must all
        // fire. Claude loads the one skill it judges most relevant and gets on with the work;
        // demanding that every plausible skill load at once measures nothing real and turns
        // correct routing into a failure. So: at least one acceptable skill, and none outside.
        const want = new Set(r.expected);
        const got = new Set(r.actual);
        const hit = [...got].filter((s) => want.has(s));
        const outside = [...got].filter((s) => !want.has(s));
        r.verdict = r.auth_error ? "AUTH-ERROR"
          : want.size === 0 ? (got.size === 0 ? "PASS" : "FALSE-POSITIVE")
          : hit.length === 0 ? (got.size === 0 ? "NO-TRIGGER" : "WRONG-SKILL")
          : outside.length ? "PASS+extra" : "PASS";
        r.hit = hit;
        r.outside = outside;
        console.log(`[${String(r.id).padStart(2)}] ${r.verdict.padEnd(14)} got=[${r.actual.map((s) => s.replace("barbatos-", "")).join(", ")}]`);
      }
    }),
  );

  results.sort((a, b) => a.id - b.id);

  if (results.some((r) => r.auth_error)) {
    console.error(
      "\nABORTED: the CLI could not authenticate, so every result above is meaningless - " +
      "an unauthenticated run looks exactly like 'no skill triggered'.\n" +
      "Run `claude` in your own terminal, `/login`, then re-run this script.",
    );
    process.exitCode = 1;
  }

  const counts = results.reduce((acc, r) => ((acc[r.verdict] = (acc[r.verdict] ?? 0) + 1), acc), {});
  const scored = results.filter((r) => r.verdict !== "AUTH-ERROR");
  const passed = scored.filter((r) => r.verdict.startsWith("PASS")).length;

  console.log(`\n${"=".repeat(58)}`);
  for (const [k, v] of Object.entries(counts).sort()) console.log(`${k.padEnd(16)} ${v}`);
  if (scored.length) {
    console.log(`${"-".repeat(58)}`);
    console.log(`pass rate        ${passed}/${scored.length} (${Math.round((100 * passed) / scored.length)}%)`);
  }

  const treeAfter = treeState();
  if (treeBefore !== null && treeAfter !== null && treeBefore !== treeAfter) {
    const before = new Set(treeBefore.split("\n"));
    const touched = treeAfter.split("\n").filter((l) => l.trim() && !before.has(l));
    console.error(
      `\nWARNING: the working tree changed during this run - a session edited files through Bash:\n` +
      touched.map((l) => `  ${l}`).join("\n") +
      `\nReview and revert them (git checkout -- <path>); they are not part of any change you asked for.`,
    );
  }

  mkdirSync(resolve(here, "results"), { recursive: true });
  const out = resolve(here, "results", `trigger-${new Date().toISOString().replace(/[:.]/g, "-")}.json`);
  writeFileSync(out, JSON.stringify({ model: MODEL, ran_at: new Date().toISOString(), counts, results }, null, 2));
  console.log(`\nresults -> ${out}`);
}

main();
