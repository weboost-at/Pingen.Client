# Pingen.Client — Spec-Sync Plan

A standalone plan for keeping the published client aligned with the live Pingen v2 OpenAPI spec.
It is independent of the original implementation plan: everything an agent needs to build the
tooling is in this file, and everything a *recurring* sync run needs is in the committed runbook
(§6) — scheduled runs never read this plan.

Working-document rules (same as the implementation plan): this file lives at
`.tmp/SPEC_SYNC_PLAN.md` in the agent's workspace, is never committed, and must never enter
`main`'s history. The house style and commit grammar are in the repo's committed `CLAUDE.md`.

---

## §1 Shape of the solution

The spec (5.5 MB) is never committed. Instead the repo commits a **fingerprint manifest**: one
digest per operation, computed over the contract-relevant parts only. A sync run is then:

```
download spec → re-fingerprint → diff against committed manifest
  ├─ no drift  → report one line, end (near-zero cost, no writes, no PR)
  └─ drift     → reconcile ONLY the drifted operations → one branch, one PR per sync run
```

The expensive part (an agent reading spec slices and editing code) happens only when Pingen
actually changed something, and only for the operations that changed.

## §2 Committed footprint (everything the repo carries)

```
tools/
├── SpecSync.cs          # single-file C# app: `dotnet run tools/SpecSync.cs -- <mode>`
│                        #   net10 file-based app — no csproj, NOT in the solution, zero packages
├── spec-manifest.json   # the contract fingerprint (~12 KB)
└── SPEC_SYNC.md         # the runbook (§6, committed verbatim) — what scheduled runs read
```

Plus two lines appended to the repo `CLAUDE.md` (§7). Nothing else; CI workflows stay untouched
(the existing ci.yml already tests every PR, including sync PRs).

## §3 The manifest (`tools/spec-manifest.json`)

```json
{
  "specTitle": "Pingen by Pingen GmbH",
  "specVersion": "2.0.0",
  "specUrl": "https://api.pingen.com/documentation/swagger-docs",
  "lastSync": "2026-08-22",
  "generatedFrom": "sha256:<digest of the raw spec bytes>",
  "authDigest": "<digest of canonicalized components.securitySchemes>",
  "emptyPathCount": 180,
  "operations": [
    {
      "id": "letters.send",
      "method": "PATCH",
      "path": "/organisations/{organisationId}/deliveries/letters/{letterId}/send",
      "kind": "operation",
      "digest": "a1b2c3d4e5f6",
      "sdk": "LetterService.SendAsync — src/Pingen.Client/Pingen.Client/Deliveries/Letters/LetterService.cs",
      "notes": null
    }
  ]
}
```

- `specUrl` is authoritative (confirmed by the repo owner): `https://api.pingen.com/documentation/swagger-docs`.
- `kind` is `"operation"` (49 entries) or `"webhook-payload"` (the five documentation-only
  `POST /your-webhook-url-for-*` paths — payload model drift matters too). 54 entries total.
- `sdk` maps each entry to its implementing method and source file — this is what makes a drift
  report actionable without archaeology. Seed it from the real implementation.
- `notes` records ACCEPTED divergences between the SDK and the spec (the known spec quirks the
  client deliberately deviates from, e.g. LetterMetaData fields modeled optional although the
  spec marks all seven required; the bogus `required:[""]` artifacts; `Location` header typed
  `number`). A note means: known and deliberate — do not re-reconcile it on every run. A note
  never suppresses drift detection; it only explains why code and spec differ.
- `emptyPathCount`: number of operation-less path stubs, so a stub gaining operations surfaces.

## §4 Fingerprinting rules (digest stability)

Per entry, digest = first 12 hex chars of SHA-256 over canonical JSON of the operation object after:

1. resolving every `$ref` inline (cycle-safe: a revisited ref renders as `{"$cycle":"<ref>"}`);
2. stripping prose and cosmetics at every depth: `description`, `summary`, `example`, `examples`,
   `title`, and all `x-*` keys — documentation rewording must NEVER fire a drift alarm;
3. sorting all object keys, sorting `parameters` by (`in`,`name`), sorting `required` arrays;
4. serializing without whitespace.

`authDigest` applies the same canonicalization to `components.securitySchemes`.

## §5 The tool (`tools/SpecSync.cs`)

```
dotnet run tools/SpecSync.cs -- check  [--spec <path|url>]     # diff; exit 0 in-sync, 1 drift, 2 spec unobtainable
dotnet run tools/SpecSync.cs -- update [--spec <path|url>]     # regenerate manifest; preserves sdk/notes per id;
                                                               #   self-verifies by re-running check (must be clean)
dotnet run tools/SpecSync.cs -- show <id> [--spec <path|url>]  # print one resolved, stripped operation (the slice an
                                                               #   agent reads instead of the spec)
```

- `--spec` default: `.tmp/swagger-docs.json` if present, else download from the manifest's
  `specUrl` (saving to `.tmp/swagger-docs.json`). Download failure → exit 2 with a one-line reason
  (never a stack trace) — the scheduled run reports it and stops.
- `check` prints a terse human table (MATCH count, then one line per CHANGED/ADDED/REMOVED with
  the `sdk` mapping, then NOTED count and AUTH status) and, when drift exists, also writes
  `.tmp/spec-drift.json` — machine-readable grouping `{changed:[…], added:[…], removed:[…],
  authChanged:bool}` with per-id `method/path/sdk/notes`. Agents work from this small file plus
  targeted `show` calls; they never open the raw spec.
- Zero dependencies beyond the BCL (System.Text.Json, SHA256, HttpClient).

## §6 The runbook — commit VERBATIM as `tools/SPEC_SYNC.md`

```markdown
# Spec sync runbook

Keeps Pingen.Client aligned with the live Pingen v2 spec. One sync = one branch = one PR.
The spec itself is NEVER committed; only tools/spec-manifest.json changes.

## Run

1. From a clean, up-to-date `main`: `dotnet run tools/SpecSync.cs -- check`
   (downloads the spec from the manifest's specUrl into .tmp/ unless .tmp/swagger-docs.json exists).
2. Exit 0 → done. Report "in sync (<N> operations, <M> noted deviations)". No branch, no PR, no writes.
   Exit 2 → the spec could not be downloaded (network policy or endpoint change). Report exactly
   that and stop — do not guess at URLs, do not open a PR.
3. Exit 1 → reconcile, grouped per run:
   a. Branch from main: `spec-sync/<YYYY-MM-DD>` (suffix `-2` if taken).
   b. Read `.tmp/spec-drift.json`. For each listed id — and ONLY those:
      `dotnet run tools/SpecSync.cs -- show <id>`, open the mapped `sdk` file, and reconcile:
      - contract change → update models/methods/tests to match the spec;
      - deliberate divergence → record/extend the manifest `notes` instead of changing code;
      - ADDED operation → implement it (this client stays feature-complete by policy);
      - REMOVED operation → mark the SDK method `[Obsolete]` with the removal date; delete it
        only in a later major.
   c. `dotnet test -c Release` on src/Pingen.Client/Pingen.Client.sln — must be green.
   d. `dotnet run tools/SpecSync.cs -- update`, confirm `check` now exits 0.
   e. Commit per coherent group in the owner's grammar (see CLAUDE.md), manifest included, e.g.
      `Updated LetterService.cs and spec-manifest.json, letters.send gained the paper_types attribute upstream`
   f. Push and open ONE pull request against main:
      - title: `Spec sync <YYYY-MM-DD>: <c> changed, <a> added, <r> removed`
      - body: one section per category; per operation one bullet — id, wire change in one
        sentence, SDK change in one sentence, or "recorded as accepted deviation". End with the
        test summary and the manifest version bump. Do not paste spec JSON into the PR.
4. Never commit anything under .tmp/. Never edit .github/workflows/**. Never push to main directly.
```

## §7 CLAUDE.md addition (append verbatim to the repo CLAUDE.md)

```markdown
## Spec sync

tools/spec-manifest.json pins the Pingen contract snapshot this client implements; the spec itself
is never committed. To check or reconcile drift, follow tools/SPEC_SYNC.md exactly.
```

## §8 Scheduling: autonomous every 30 days + manual trigger

Primary mechanism — a **Claude Code Routine** (cloud scheduled trigger), created AFTER the sync
tooling is merged to `main`:

- schedule: monthly (`0 6 1 * *` UTC — "every thirty days or so"), each firing starts a FRESH
  session in the repo's environment;
- the Routine prompt is intentionally tiny — the committed runbook carries the process:

```
Scheduled Pingen spec sync for weboost-at/Pingen.Client. Start from a clean, up-to-date main.
If tools/SPEC_SYNC.md is missing on main, report "sync tooling not merged" and stop.
Otherwise follow tools/SPEC_SYNC.md exactly and end your run with its report line
(or the PR link if drift was reconciled).
```

- manual trigger: fire the same Routine on demand from any Claude Code session ("run the Pingen
  spec sync Routine now"), or paste the same three-line prompt into a fresh session, or run
  `dotnet run tools/SpecSync.cs -- check` yourself locally for a zero-agent answer.

Token-cost profile: the in-sync path is one clone + one command + a one-line report — the agent
reads the runbook (~40 lines) and a one-line tool result, nothing else. The drift path scales with
the number of drifted operations only (small `spec-drift.json` + per-id `show` slices), never with
spec size.

Prerequisite: the session environment's network policy must allow
`https://api.pingen.com` (the planning environment currently returns a proxy 403 for it — allow
the host in the environment settings, or scheduled runs will exit 2 and report "spec unobtainable").

Alternative (only if Routines are unavailable): a `schedule:`+`workflow_dispatch` GitHub Actions
workflow invoking the Claude Code action with the same three-line prompt — costs a workflow file
and an API-key secret, so the Routine is preferred.

## §9 Build order & acceptance (for the implementing agent)

One sub-agent, one commit on the implementation branch:

1. Write `tools/SpecSync.cs` per §4/§5.
2. Run `update` against the current spec (`.tmp/swagger-docs.json`, or download from the §3 URL).
3. Fill every `sdk` mapping from the real implementation (54 entries) and seed `notes` with the
   known deliberate divergences visible in the code (search XML docs/comments for spec-quirk
   remarks; at minimum: LetterMetaData optional fields, the `Location`-header `number` mistype,
   the `required:[""]` artifacts, letters price-calculator null-on-202).
4. Commit `tools/SPEC_SYNC.md` (§6 verbatim) and append §7 to `CLAUDE.md` in the same commit.
5. Acceptance, all mandatory before committing:
   - `update` then `check` exits 0 (idempotence);
   - editing only a `description` in a spec copy → `check` still exits 0;
   - deleting one attribute from a request schema in a spec copy → exits 1 and names the
     operation with its `sdk` mapping;
   - unreachable `specUrl` with no `.tmp/` spec → exits 2 with a one-line reason;
   - `dotnet test -c Release` untouched and green (the tool is not part of the solution).
6. Commit message: `Added SpecSync.cs, spec-manifest.json and SPEC_SYNC.md, drift against the Pingen spec now surfaces per operation and monthly syncs open one grouped pull request`
