#!/usr/bin/env node
// Measures documentation drift in Barbatos.Wpf - the checks the barbatos-docs-sync skill asks
// for, so nobody has to eyeball a 1,800-line README against its menu.
//
// Usage (from anywhere in the repo):
//   node .claude/skills/barbatos-docs-sync/check-docs.mjs           menus, links, anchors,
//                                                                   namespace coverage
//   node .claude/skills/barbatos-docs-sync/check-docs.mjs --types   ...plus public types
//                                                                   missing from API-REFERENCE
//
// Output kinds: BROKEN (link/anchor/namespace section missing), MENU (Documentation Menu out of
// step with the headings), NUGET (a relative link, or an image host nuget.org does not allow, in
// a package README - both render empty on the nuget.org package page), ROOT (namespace missing from the root API-REFERENCE.md),
// UNDOCUMENTED (--types only; informational).
//
// Dependency-free on purpose: plain Node, no npm install (Python may not be installed).
// Exit code 1 when anything but an UNDOCUMENTED line is printed.
//
// Heading slugs follow GitHub: rendered heading text, lowercased, every character that is not
// a letter, digit, mark, space, "-" or "_" dropped, spaces turned into "-", and a repeated slug
// suffixed "-1", "-2", ... in document order.

import { existsSync, readFileSync, readdirSync } from "node:fs";
import { dirname, join, relative, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const repo = resolve(dirname(fileURLToPath(import.meta.url)), "..", "..", "..");
const packages = ["Core", "Aquarius", "AquariusRouter", "AquariusValee"].map(
  (p) => `src/Barbatos.Wpf.${p}`,
);
const readmes = packages.map((p) => `${p}/README.md`);
const docs = [
  ...readmes,
  ...packages.map((p) => `${p}/API-REFERENCE.md`),
  "README.md",
  "API-REFERENCE.md",
  "templates/README.md",
];
const gitHub =
  /^https:\/\/github\.com\/Barbatos-Labs\/Barbatos\.Wpf(?:\/(?:blob|tree)\/main\/([^?#]*))?\/?(?:\?[^#]*)?(?:#(.*))?$/;

// The image hosts from NuGet's "Package readme on NuGet.org" allow-list that this repo could use.
// Anything else - github.com/.../blob/...?raw=true included - is rendered as <img src=""> on the
// package page (seen on the published Barbatos.Wpf pages).
const nugetImageHosts = new Set([
  "img.shields.io",
  "raw.githubusercontent.com",
  "raw.github.com",
  "media.githubusercontent.com",
  "user-images.githubusercontent.com",
  "avatars.githubusercontent.com",
  "camo.githubusercontent.com",
  "badgen.net",
  "flat.badgen.net",
  "cdn.jsdelivr.net",
  "i.imgur.com",
]);

let failures = 0;
const say = (kind, file, line, message) => {
  if (kind !== "UNDOCUMENTED") failures++;
  console.log(`${kind.padEnd(12)} ${file}${line ? `:${line}` : ""}  ${message}`);
};

const slug = (text) =>
  text
    .replace(/\[([^\]]*)\]\([^)]*\)/g, "$1")
    .toLowerCase()
    .replace(/[^\p{L}\p{N}\p{M} _-]/gu, "")
    .replace(/ /g, "-");

const parsed = new Map();
function parse(file) {
  if (parsed.has(file)) return parsed.get(file);
  const lines = readFileSync(join(repo, file), "utf8").split(/\r?\n/);
  const heads = [];
  const seen = new Map();
  let fence = false;
  lines.forEach((line, i) => {
    if (/^\s*```/.test(line)) {
      fence = !fence;
      return;
    }
    const m = !fence && /^(#{1,6})\s+(.*?)\s*#*\s*$/.exec(line);
    if (!m) return;
    const base = slug(m[2]);
    const n = seen.get(base) ?? 0;
    seen.set(base, n + 1);
    heads.push({ level: m[1].length, text: m[2], slug: n ? `${base}-${n}` : base, line: i + 1 });
  });
  const result = { lines, heads, slugs: new Set(heads.map((h) => h.slug)) };
  parsed.set(file, result);
  return result;
}

// 1. Every link that stays inside this repository resolves, anchor included.
for (const file of docs) {
  const { lines } = parse(file);
  const isPackageReadme = readmes.includes(file);
  let fence = false;
  lines.forEach((raw, i) => {
    if (/^\s*```/.test(raw)) {
      fence = !fence;
      return;
    }
    if (fence) return;
    const line = raw.replace(/`[^`]*`/g, "");
    if (isPackageReadme) {
      const images = [
        ...[...line.matchAll(/!\[[^\]]*\]\(([^)\s]+)\)/g)].map((m) => m[1]),
        ...[...line.matchAll(/<img[^>]*\ssrc="([^"]+)"/g)].map((m) => m[1]),
      ];
      for (const src of images) {
        const host = /^https:\/\/([^/]+)/.exec(src)?.[1];
        if (!host || !nugetImageHosts.has(host)) {
          say("NUGET", file, i + 1, `image ${src} is not on nuget.org's image allow-list - it renders blank there`);
        }
      }
    }
    const targets = [
      ...[...line.matchAll(/\]\(([^)\s]+)\)/g)].map((m) => m[1]),
      ...[...line.matchAll(/href="([^"]+)"/g)].map((m) => m[1]),
    ];
    for (const target of targets) {
      let path;
      let anchor;
      const gh = gitHub.exec(target);
      if (gh) {
        path = gh[1] === undefined ? "README.md" : gh[1];
        anchor = gh[2];
        if (path === "" || path.endsWith("/")) continue;
      } else if (/^[a-z]+:/i.test(target)) {
        continue;
      } else {
        const [part, hash] = target.split("#");
        path = part ? relative(repo, resolve(repo, dirname(file), part)).replaceAll("\\", "/") : file;
        anchor = hash;
        if (part && isPackageReadme) {
          say("NUGET", file, i + 1, `relative link ${target} does not resolve on nuget.org - use the absolute GitHub URL`);
        }
      }
      if (!existsSync(join(repo, path))) {
        say("BROKEN", file, i + 1, `${target} -> no file ${path}`);
      } else if (anchor && anchor !== "readme" && path.endsWith(".md") && !parse(path).slugs.has(anchor)) {
        say("BROKEN", file, i + 1, `${target} -> no heading #${anchor} in ${path}`);
      }
    }
  });
}

// 2. Each package README's Documentation Menu matches its ## / ### headings, in order.
for (const file of readmes) {
  const { lines, heads } = parse(file);
  const at = heads.findIndex((h) => h.text.includes("Documentation Menu"));
  if (at < 0) {
    say("MENU", file, 0, "no Documentation Menu heading");
    continue;
  }
  const bodyStart = heads[at + 1]?.line ?? lines.length;
  const byslug = new Map(heads.map((h) => [h.slug, h]));
  const entries = [];
  for (let n = heads[at].line; n < bodyStart - 1; n++) {
    const m = /^(\s*)[*-]\s+.*?\]\(#([^)]+)\)/.exec(lines[n]);
    if (m) entries.push({ depth: Math.floor(m[1].length / 2), anchor: m[2], line: n + 1 });
  }
  const listed = new Set(entries.map((e) => e.anchor));
  const parentAt = [];
  let last = 0;
  for (const e of entries) {
    const h = byslug.get(e.anchor);
    parentAt[e.depth] = e.anchor;
    if (!h) {
      say("BROKEN", file, e.line, `menu entry #${e.anchor} matches no heading`);
      continue;
    }
    if (h.level !== e.depth + 2) {
      say("MENU", file, e.line, `#${e.anchor} is nested at menu depth ${e.depth} but is a level-${h.level} heading`);
    }
    if (h.line < last) say("MENU", file, e.line, `#${e.anchor} comes before the previous entry in the body`);
    last = h.line;
    if (e.depth > 0) {
      const realParent = heads.filter((p) => p.line < h.line && p.level < h.level).at(-1);
      if (realParent && realParent.slug !== parentAt[e.depth - 1]) {
        say("MENU", file, e.line, `#${e.anchor} is listed under #${parentAt[e.depth - 1]} but lives under #${realParent.slug}`);
      }
    }
  }
  for (const h of heads) {
    if (h.line > bodyStart - 1 && (h.level === 2 || h.level === 3) && !listed.has(h.slug)) {
      say("MENU", file, h.line, `${"#".repeat(h.level)} ${h.text} has no menu entry (#${h.slug})`);
    }
  }
}

// 3. Every namespace that holds a public type has its own section in the package
//    API-REFERENCE.md and is listed in the root API-REFERENCE.md. With --types, also list the
//    public types the package API-REFERENCE.md never names - candidates, not verdicts: a public
//    type nested in an internal one, or an extension-method holder whose methods are documented,
//    shows up too.
const walk = (dir) =>
  readdirSync(dir, { withFileTypes: true }).flatMap((d) =>
    d.isDirectory()
      ? d.name === "bin" || d.name === "obj"
        ? []
        : walk(join(dir, d.name))
      : d.name.endsWith(".cs")
        ? [join(dir, d.name)]
        : [],
  );
const decl =
  /^\s*public\s+(?:(?:sealed|static|abstract|partial|readonly|ref|unsafe|new)\s+)*(?:class|interface|record(?:\s+struct|\s+class)?|enum|struct|delegate\s+[\w<>?,[\]. ]+?)\s+(\w+)/gm;
const rootReference = readFileSync(join(repo, "API-REFERENCE.md"), "utf8");
for (const pkg of packages) {
  const referenceFile = `${pkg}/API-REFERENCE.md`;
  const reference = readFileSync(join(repo, referenceFile), "utf8");
  const sections = parse(referenceFile).heads.filter((h) => h.level === 2).map((h) => h.text);
  const names = new Set();
  const namespaces = new Set();
  for (const cs of walk(join(repo, pkg))) {
    const source = readFileSync(cs, "utf8");
    const found = [...source.matchAll(decl)].map((m) => m[1]);
    found.forEach((t) => names.add(t));
    const ns = /^namespace\s+([\w.]+)/m.exec(source)?.[1];
    if (ns && found.length) namespaces.add(ns);
  }
  for (const ns of [...namespaces].sort()) {
    if (!sections.some((t) => t.includes(`\`${ns}\``))) {
      say("BROKEN", referenceFile, 0, `public namespace ${ns} has no "## ... ${ns} ..." section`);
    }
    if (!new RegExp(`\`${ns.replaceAll(".", "\\.")}\``).test(rootReference)) {
      say("ROOT", "API-REFERENCE.md", 0, `public namespace ${ns} is not listed`);
    }
  }
  if (process.argv.includes("--types")) {
    const missing = [...names].filter((t) => !new RegExp(`\\b${t}\\b`).test(reference)).sort();
    if (missing.length) say("UNDOCUMENTED", referenceFile, 0, missing.join(", "));
  }
}

console.log(failures ? `\n${failures} problem(s).` : "\nNo problems.");
process.exit(failures ? 1 : 0);
