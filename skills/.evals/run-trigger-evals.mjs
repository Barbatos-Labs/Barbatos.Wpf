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

import { spawn } from "node:child_process";
import { readFileSync, writeFileSync, mkdirSync } from "node:fs";
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

const suite = JSON.parse(readFileSync(resolve(here, "trigger-evals.json"), "utf8"));
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

    const child = spawn(
      "claude",
      ["-p", "--model", MODEL, "--output-format", "stream-json", "--verbose", "--include-partial-messages"],
      { cwd: repoRoot, env, shell: true },
    );
    child.stdin.write(q.query);
    child.stdin.end();

    const skills = new Set();
    let firstTool = null;
    let pendingTool = null;
    let partial = "";
    let buf = "";
    let settled = false;
    let authError = false;

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
        auth_error: authError,
      });
    };

    const timer = setTimeout(finish, TIMEOUT_MS);

    const noteSkill = (raw) => {
      const m = raw.match(/barbatos-[a-z-]+/g);
      if (m) m.forEach((s) => skills.add(s));
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
          const se = ev.event ?? {};
          if (se.type === "content_block_start" && se.content_block?.type === "tool_use") {
            pendingTool = se.content_block.name ?? null;
            firstTool ??= pendingTool;
            partial = "";
          } else if (se.type === "content_block_delta" && pendingTool === "Skill") {
            partial += se.delta?.partial_json ?? "";
            noteSkill(partial);
          } else if (se.type === "content_block_stop") {
            if (pendingTool === "Skill") noteSkill(partial);
            // A skill fired, or the model committed to a different tool first: either way
            // the routing decision is already made.
            if (firstTool) finish();
            pendingTool = null;
          } else if (se.type === "message_stop") {
            finish();
          }
        } else if (ev.type === "assistant") {
          const tools = (ev.message?.content ?? []).filter((c) => c.type === "tool_use");
          for (const c of tools) {
            firstTool ??= c.name;
            if (c.name === "Skill") noteSkill(JSON.stringify(c.input ?? {}));
          }
          // Only a tool call settles the question here. A tool-free reply looks identical
          // whether Claude genuinely chose to answer unaided or the CLI failed to
          // authenticate, so wait for `result` - which carries is_error - before scoring it.
          if (tools.length) finish();
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

async function main() {
  console.log(`Running ${queries.length} queries against ${MODEL} (concurrency ${CONCURRENCY})\n`);
  const results = [];
  const queue = [...queries];

  await Promise.all(
    Array.from({ length: Math.min(CONCURRENCY, queue.length) }, async () => {
      while (queue.length) {
        const q = queue.shift();
        const r = await runQuery(q);
        results.push(r);
        const want = new Set(r.expected);
        const got = new Set(r.actual);
        const missed = [...want].filter((s) => !got.has(s));
        const extra = [...got].filter((s) => !want.has(s));
        r.verdict = r.auth_error ? "AUTH-ERROR"
          : want.size === 0 ? (got.size === 0 ? "PASS" : "FALSE-POSITIVE")
          : missed.length === 0 ? (extra.length ? "PASS+extra" : "PASS")
          : got.size === 0 ? "NO-TRIGGER" : "WRONG-SKILL";
        r.missed = missed;
        r.extra = extra;
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

  mkdirSync(resolve(here, "results"), { recursive: true });
  const out = resolve(here, "results", `trigger-${new Date().toISOString().replace(/[:.]/g, "-")}.json`);
  writeFileSync(out, JSON.stringify({ model: MODEL, ran_at: new Date().toISOString(), counts, results }, null, 2));
  console.log(`\nresults -> ${out}`);
}

main();
