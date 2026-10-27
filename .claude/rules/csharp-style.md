---
paths:
  - "src/**/*.cs"
  - "tests/**/*.cs"
  - "samples/**/*.cs"
  - "templates/**/*.cs"
  - ".editorconfig"
---

# C# style in this repo

`.editorconfig` is the authority; these are the parts that are easy to get wrong.

- **File header** on every `.cs` file in `src/` and `tests/` (`.editorconfig` makes `IDE0073`
  an error, but only in the IDE — the build does not enforce code style, so check by eye), as `//`
  line comments with this exact text:

  ```csharp
  // This Source Code Form is subject to the terms of the MIT License.
  // If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
  // Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
  // All Rights Reserved.
  ```

  Copy it from any file under `src/` (or a Core test) rather than retyping it. `samples/` is
  mixed — match the neighbouring files. `templates/` never carries it: the generated files belong
  to the user's app.
- File-scoped namespaces. `_camelCase` private fields, `PascalCase` for everything public,
  `I`-prefixed interfaces, `Async` suffix on async methods.
- Braces on their own line (Allman). No multiple blank lines, no blank line between two
  consecutive braces, and a blank line after a closing `}` before the next statement.
- `Nullable` and `ImplicitUsings` come from `Directory.Build.props` for `src/` and `tests/` —
  never set them in those project files, and never disable nullability locally (`#nullable`).
  The sample apps and the project template set both explicitly on purpose: they model a
  standalone consumer app.
- Match the surrounding file for local-variable style: `var` is used throughout `src/` even
  though `.editorconfig` (`csharp_style_var_elsewhere = false:warning`) says otherwise — follow
  the code, and never rewrite existing `var` declarations to satisfy that setting. Likewise
  collection expressions (`= []`, `[.. items]`), primary constructors for small internal types,
  and pattern matching over `as` + null check.
- Comments explain decisions, trade-offs and what was verified empirically. A comment that
  restates the code does not belong here.
- **Editing `.editorconfig` itself:** never change `file_header_template` (every existing header
  would become an `IDE0073` error); keep `IDE0130` at `none` (Core folders do not match its
  namespaces on purpose); do not lower a severity to silence a warning — fix the code, or add a
  commented `NoWarn` in the `.csproj` (`build-and-packages`).
