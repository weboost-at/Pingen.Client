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
