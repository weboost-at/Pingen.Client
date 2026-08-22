# Pingen.Client — house rules

.NET client library for the Pingen v2 API. net10.0, nullable enabled, DI-native, no framework coupling.
Full architecture and wire contracts: .tmp/IMPLEMENTATION_PLAN.md (gitignored working document — never commit it; sections are numbered, read what your task names).

## Style (binding)

- File-scoped namespaces; namespace = folder path rooted at `Pingen.Client`.
- One concept per file, named after its central type; closely-coupled satellite types live in the
  same file (an options record's nested section, a service's small response records).
- Records by default (`{ get; init; }`, `required` for wire-required members); classes only where
  behavior or mutable state lives; primary constructors, use parameters directly.
- No `sealed`, no `#region`, no service interfaces, no ceremony. Public or private — avoid
  `internal` except the request core on `PingenClient`.
- `var` for locals; expression bodies for any single-expression member; brace-less one-line guards
  (`if (x is null) return …;`); `is null` / `is not null` — never `== null`; switch expressions
  with a `_` arm; collection expressions (`[]`, `[item]`, spreads); target-typed `new`; named
  arguments on multi-argument constructions, one per line, closing `)` on its own line.
- `_camelCase` private fields; PascalCase consts and static readonly fields.
- Extension members: C# 14 `extension(...)` blocks for public extensions; classic `this` parameters
  only for `IServiceCollection`-style config extensions and private helpers.
- Async: `Async` suffix on every async method; `CancellationToken cancellationToken = default` as
  the last parameter of every public async API; `await using` for async disposables; no
  `.Result`/`.Wait()`; no `ConfigureAwait` (net10.0-only library, no sync-context consumers).
- JSON: explicit `[JsonPropertyName("snake_case")]` on every wire property — never rely on a naming
  policy; `JsonSerializerOptions` cached in `private static readonly` fields; System.Text.Json only.
- XML docs: one-sentence `<summary>` on every public type and member — NEVER on private members or
  the internal request core; document defaults on options (`default is <c>587</c>` style); no
  `<param>` on record positional parameters, no `<remarks>` essays.
- Comments: one-liners that state a constraint, hazard, or why — never what the next line does.
  Dash punctuation ("Constant-time - == leaks…"). Comment as a last resort.
- Don't defend against states that can't occur — a null check no call path can trigger is
  misinformation, not safety. Don't duplicate — promote shared helpers to `Common/`. Inline
  what's used once.

## Tests (binding)

- xunit.v3 + FluentAssertions 7 (`Should()`); test files mirror source folders; names
  `When_<condition>_<Method>_<expected>` or `Given_…_When_…_It_…` in underscore form.
- Only `// Arrange`, `// Act`, `// Assert` comments inside tests. `[Theory]`/`[InlineData]` for
  value tables (include unicode/edge values). `TestContext.Current.CancellationToken` when passing tokens.
- Tests go through real DI via the shared `PingenTestHost` + `RecordingHandler` (Tests/ folder);
  never mock HttpClient with a mocking framework. Until phase 8 wires service registrations,
  construct the service under test from the DI-resolved client (`new LetterService(host.Client)`).
- Prefer fewer, denser tests; shared setup in test bases; never wrap a bug in a green test.

## Commits

`<PastTenseVerb> <File.ext>[, <File2.ext> and <File3.ext>], <lowercase behavior clause>` — verbs:
Added/Updated/Fixed/Refactored/Moved/Dropped; backticked identifiers; rationale with "so"/"since";
no trailing period. Example: `Added PingenAccessTokens.cs, tokens cache for 12 hours and refresh 60 seconds early`

Commit and push after every coherent unit of work (a phase) — never batch the implementation into
one drop. If a branch is used at all, it is a single `feature/<topic>` branch for the whole job.

## Boundaries

- Never edit `.github/workflows/**`, never commit `.tmp/**`, never add dependencies beyond the set
  in .tmp/IMPLEMENTATION_PLAN.md §2.
- `README.md` at repo root is packed into the NuGet package — `dotnet pack` fails without it.

## Spec sync

tools/spec-manifest.json pins the Pingen contract snapshot this client implements; the spec itself
is never committed. To check or reconcile drift, follow tools/SPEC_SYNC.md exactly.
