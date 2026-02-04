You are an expert SSMS/Visual Studio extension engineer and T-SQL tooling architect.

Goal: Build "SQL Essentials", an SSMS add-in providing:
- context-aware autocomplete
- smart JOIN suggestions (FK-first, heuristic fallback)
- deterministic SQL formatting with profiles + CLI
- safe refactoring (rename/expand wildcards/qualify objects/etc.)
- rule-based code analysis with profiles and diagnostics UI
- optional AI assistant layer (opt-in, privacy-first)

Constraints:
- SSMS is evolving to a Visual Studio 2022-based, 64-bit shell; design for modern VS extensibility patterns while keeping a compatibility strategy.
- Avoid blocking the UI thread; async everything that touches metadata or AI.
- All transformations must be previewable as diffs and undoable as a single unit.
- AI is optional and must be off by default; provide transparency about any data sent.

Work phases:
1) Architecture & repo setup
   - Propose solution structure: ExtensionHost project + CoreEngine library + CLI tools (formatter/analyzer).
   - Define interfaces: IParser, ISemanticModel, IMetadataProvider, ICompletionEngine, IFormatter, IRefactoringEngine, IAnalyzer, ISettingsStore.
   - Add dependency decisions: ScriptDOM + SMO.

2) Minimal vertical slice (MVP)
   - Hook into editor events (buffer changed, caret moved).
   - Implement completion provider with:
        - lexical fallback suggestions
        - schema-aware suggestions via async metadata cache
        - deterministic ranking (type + prefix + MRU)
   - Add a small tool window/panel to show diagnostics.

3) Formatter MVP + CLI
   - Implement AST-driven formatter wrapper with a small set of style options.
   - Implement profile persistence (JSON) and load/save in UI.
   - Build formatter CLI: format-files, check-mode (exit code if changes).

4) Refactoring MVP
   - Implement rename-alias (scope-aware, AST-based rewrite).
   - Implement expand wildcards using metadata (SELECT * -> explicit column list).
   - Implement qualify object names where resolvable.

5) Analyzer MVP
   - Implement 10–15 initial rules (naming, SELECT *, implicit schema, etc.).
   - Implement profiles (enable/disable rules, severity), stored on disk.
   - Emit diagnostics with stable rule IDs; add “learn more” links to local docs.

6) Optional AI layer (phase-gated)
   - Implement an AI policy layer:
        - redaction controls
        - schema-only mode
        - per-feature opt-in toggles
   - Implement: Explain Selected SQL + Fix Selected SQL (return suggested diff; user must apply).
   - Implement AI completions as a separate completion stream (clearly labeled).

7) Testing & release engineering
   - Unit tests for ranking, join generation, refactoring rewrites, rule evaluations.
   - Golden-file tests for formatting (input -> expected output per profile).
   - Performance tests: completion latency under large schemas.
   - Packaging: VSIX (or appropriate installer) and versioning.
   - Generate a developer guide and a user guide.

Deliverables:
- A finalized architecture doc with diagrams
- Backlog of user stories + acceptance criteria
- Source code for MVP completion + formatter + refactoring + analyzer
- Test plan + CI pipeline outline
- Release packaging plan and compatibility notes for SSMS versions
