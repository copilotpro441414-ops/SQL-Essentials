## 1. Target Scope for SQL Essentials

### 1.1 Primary Objectives (MVP)

1. **Fast, context‑aware autocomplete**
   - Objects: databases, schemas, tables, views, columns, procedures, functions, synonyms. [web:31][web:29]
   - Aware of current clause (SELECT/FROM/WHERE/JOIN/etc.). [web:31][web:29]
   - Filters suggestions by active schema/connection. [web:31][web:29]

2. **Smart JOIN completion**
   - Detects current JOIN context, left/right tables, aliases.
   - Suggests `ON` predicates based on foreign keys and column name patterns, similar to competing tools. [web:28][web:20]

3. **Alias‑aware column suggestions**
   - Understands aliases defined in FROM/JOIN.
   - When typing `<alias>.`, suggests only that table’s columns.

4. **Snippet system**
   - Built‑in snippets for common patterns (SELECT, INSERT, UPDATE, DELETE, CTE, TRY/CATCH).
   - Custom snippet editor and storage.
   - Tab‑stops/placeholder navigation.

5. **Basic formatting**
   - At least one built‑in profile (opinionated, “sane default”).
   - Format document / selection command.

6. **Good performance**
   - Autocomplete popup within ~50–100 ms after trigger, similar to other tools. [web:31][web:20]

### 3.2 Secondary Objectives (Phase 2+)

- Smart rename within the script (aliases, variables, temp tables).
- Basic code analysis / linting (e.g. warning on `SELECT *`, unused aliases). [web:36]
- Additional formatting profiles & fine‑grained rules. [web:29]
- Query history and “recent snippets”. [web:36]
- Team snippet sharing (file or git folder based). [web:28]

---

## 2. SSMS Extension Architecture & Constraints

Public references and community guides show that SSMS extensions are essentially Visual Studio‑style VSIX / VSPackage add‑ins in the SSMS process. [web:18][web:21][web:24][web:30]

### 2.1 Extension Model

- **Technology**: VSIX extension with a `Package` subclass (VSPackage) loaded by SSMS. [web:18][web:30]
- **Language**: C# (.NET) is standard. [web:18][web:24]
- **Entry point**: Package class decorated with registration attributes, loaded at SSMS startup. [web:30]

Example structure from community samples: [web:18][web:30]

- A VSIX project containing:
  - `source.extension.vsixmanifest`
  - `Pkgdef` and registration attributes for the Package.
  - Command classes using `OleMenuCommandService` to hook menus/toolbars. [web:30]

### 2.2 Editor Integration

Community examples and posts indicate: [web:18][web:21][web:24][web:30]

- You can access the active editor via SSMS’s VSIntegration layer and EnvDTE. [web:24][web:21]
- Text editing is done through Visual Studio editor APIs (`IWpfTextView`, `ITextBuffer`, `TextSelection`). [web:18][web:30]
- Keystrokes and caret movement can be hooked via text view events. [web:21][web:30]

Constraints:

- Extension runs in‑process; heavy work must be async and non‑blocking. [web:24]
- SSMS versions differ slightly; need to target supported versions (2016+). [web:24]

### 2.3 Metadata Access

To implement context‑aware autocomplete and JOIN suggestions, schema metadata must be fetched from the connected SQL Server database, similar to how other tools build metadata caches. [web:23][web:36]

Typical sources:

- `sys.tables`, `sys.views`, `sys.procedures`, `sys.columns`, `sys.foreign_keys`, `sys.indexes`. [web:23]
- `INFORMATION_SCHEMA` views as a compatibility layer. [web:23]

Requirements:

- Per‑connection schema cache with TTL.
- Manual refresh option.
- Avoid blocking on large schemas (async load, partial / lazy load). [web:23][web:36]

---

## 3. Functional Requirements

### 3.1 Autocomplete

**FR‑AC‑01 – Triggering**

- Autocomplete must trigger on:
  - Dot after identifier (`alias.`).
  - Space after keywords like SELECT, FROM, JOIN, WHERE.
  - Configurable shortcut (e.g., Ctrl+Space).
- User can disable/enable automatic popup.

**FR‑AC‑02 – Suggestions**

- Suggestion categories:
  - Tables, views, synonyms.
  - Columns (filtered by current FROM/JOIN context).
  - Stored procedures and functions.
  - Variables and parameters.
  - Snippets.
- Suggestions filtered by partially typed prefix.

**FR‑AC‑03 – Clause Awareness**

- In SELECT: prioritise columns and expressions. [web:31][web:29]
- In FROM/JOIN: prioritise tables/views. [web:31][web:29]
- In WHERE/HAVING: prioritise columns and scalar functions. [web:31][web:29]
- After alias + dot: only that table’s columns. [web:31][web:29]

**FR‑AC‑04 – Ranking**

- Rank suggestions by:
  - Exact prefix match.
  - Recent usage frequency.
  - Key columns (PK/FK) first.
  - Previously selected columns in current query.

### 3.2 JOIN Assistance

**FR‑JN‑01 – Detection**

- Detect when the cursor is in a JOIN clause or ON expression.

**FR‑JN‑02 – ON Clause Suggestions**

- For `JOIN <RightTable> <alias> ON`:
  - Use metadata to find FK/PK relationships between `<LeftTable>` and `<RightTable>`.
  - Suggest join predicates with highest‑confidence first, similar to competitors. [web:28][web:20]

**FR‑JN‑03 – Full Join Snippets**

- When user types `JOIN <RightTable> <alias>` and hits space/Enter:
  - Offer to insert `ON <leftAlias>.<pk> = <alias>.<fk>` automatically.

### 3.3 Snippets

**FR‑SN‑01 – Built‑in Snippets**

- Provide a starter set:
  - SELECT, INSERT, UPDATE, DELETE templates.
  - CTE, TRY/CATCH, MERGE patterns.
- Exposed via shortcuts (e.g., `sel`, `ins`). [web:28][web:29]

**FR‑SN‑02 – Custom Snippets**

- GUI to add/edit/delete snippets.
- Placeholders with tab navigation.
- Category/keyword tagging.

### 3.4 Formatting

**FR‑FM‑01 – Basic Formatter**

- At least one built‑in profile (default):
  - Uppercase keywords.
  - Indentation of nested queries.
  - Joins and predicates on new lines.

**FR‑FM‑02 – Commands**

- “Format Document” and “Format Selection” commands.
- Optional auto‑format on paste.

---

## 4. Non‑Functional Requirements

### 4.1 Performance

- Autocomplete popup:
  - Target: ≤ 50 ms from keystroke.
  - Max: 150 ms under heavy load, similar to commercial tools. [web:31][web:20]
- Schema metadata:
  - Initial load may be up to ~500 ms on large databases; must be cached. [web:23]
- Memory:
  - Per‑database schema cache should remain practical (< tens of MB for typical schemas). [web:23][web:36]

### 4.2 UX

- Keyboard‑first:
  - Arrow keys for navigation.
  - Enter/Tab to accept.
  - Esc to cancel.
- Minimal visual noise, matching SSMS theme.

### 4.3 Compatibility

- SSMS versions: primarily 2016+ (with explicit version matrix).
- SQL Server versions: 2012+ (aligned with system catalog usage). [web:23]