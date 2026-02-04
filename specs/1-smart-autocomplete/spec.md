# Feature Specification: SQL Essentials MVP — Smart Autocomplete & Productivity Core

**Feature Branch**: `1-smart-autocomplete`  
**Created**: 2026-02-04  
**Status**: Draft  
**Scope**: MVP Phase 1 of SQL Essentials (Autocomplete + JOIN + Snippets + Basic Formatting)  
**Input**: User description: "Build an SSMS plugin called SQL Essentials that provides smart, context-aware T-SQL autocomplete and JOIN assistance similar to Redgate SQL Prompt and dbForge SQL Complete."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Context-Aware Column Completion (Priority: P1)

A SQL developer is writing a SELECT statement and wants to quickly add columns from the tables already referenced in the FROM clause. After typing a table alias followed by a dot (e.g., `u.`), the system displays only the columns belonging to that specific table, ranked by relevance (primary keys first, then frequently used columns).

**Why this priority**: This is the most frequent autocomplete interaction; developers type `alias.column` hundreds of times per day. Delivering instant, accurate column suggestions provides immediate productivity gains and demonstrates core value.

**Independent Test**: Can be fully tested by connecting to any database, writing a SELECT with a FROM clause and alias, then typing `alias.` and verifying only that table's columns appear within 100ms.

**Acceptance Scenarios**:

1. **Given** a query with `SELECT * FROM Users u`, **When** user types `u.` after SELECT, **Then** system displays only columns from the Users table within 100ms.
2. **Given** a query with multiple tables (`Users u`, `Orders o`), **When** user types `o.`, **Then** system displays only columns from the Orders table.
3. **Given** the popup is visible, **When** user continues typing (e.g., `u.User`), **Then** suggestions filter to match the prefix in real-time.
4. **Given** suggestions are displayed, **When** user presses Tab or Enter on a suggestion, **Then** the column name is inserted at the cursor position.

---

### User Story 2 - Clause-Aware Object Completion (Priority: P1)

A SQL developer is writing a query and wants relevant suggestions based on the current clause context. In a FROM clause, the system prioritizes tables and views; in a SELECT clause without an alias prefix, it suggests columns from all referenced tables; in a WHERE clause, it suggests columns and scalar functions.

**Why this priority**: Clause-aware suggestions reduce noise and cognitive load. This is foundational to "smart" autocomplete and differentiates the tool from basic IntelliSense.

**Independent Test**: Can be tested by writing a query, positioning cursor in different clauses, triggering autocomplete, and verifying the suggestion categories match the clause context.

**Acceptance Scenarios**:

1. **Given** cursor is after `FROM `, **When** autocomplete triggers, **Then** tables and views are prioritized in suggestions.
2. **Given** cursor is in a SELECT clause with tables defined, **When** autocomplete triggers, **Then** columns from referenced tables are prioritized.
3. **Given** cursor is after `WHERE `, **When** autocomplete triggers, **Then** columns and comparison operators are prioritized.
4. **Given** cursor is after `JOIN `, **When** autocomplete triggers, **Then** tables and views are prioritized.

---

### User Story 3 - Smart JOIN Predicate Suggestions (Priority: P2)

A SQL developer is writing a JOIN clause and wants the system to suggest the ON predicate automatically. After specifying `JOIN Orders o ON`, the system analyzes foreign key relationships and column name patterns between the left and right tables to suggest the most likely join condition (e.g., `u.UserID = o.UserID`).

**Why this priority**: JOIN conditions require knowledge of schema relationships that developers often need to look up. Automating this saves significant time and reduces errors, but depends on having metadata access working first.

**Independent Test**: Can be tested by connecting to a database with defined foreign keys, writing a JOIN clause, and verifying the suggested ON predicate matches the FK relationship.

**Acceptance Scenarios**:

1. **Given** `SELECT * FROM Users u JOIN Orders o ON `, **When** autocomplete triggers, **Then** system suggests `u.UserID = o.UserID` if FK relationship exists.
2. **Given** no FK relationship exists between tables, **When** autocomplete triggers on JOIN ON, **Then** system suggests predicates based on matching column names (e.g., both tables have `CustomerID`).
3. **Given** multiple possible join conditions exist, **When** autocomplete triggers, **Then** system ranks FK-based suggestions higher than name-based suggestions.
4. **Given** user selects a join suggestion, **When** they press Tab/Enter, **Then** the complete predicate is inserted.

---

### User Story 4 - Keyword and Snippet Completion (Priority: P2)

A SQL developer wants to quickly insert T-SQL keywords and common code patterns. When typing the beginning of a keyword (e.g., `SEL`), the system suggests `SELECT`. When typing a snippet shortcut (e.g., `selj`), the system offers to insert a complete SELECT...JOIN template with placeholders.

**Why this priority**: Reduces keystrokes for common patterns. Snippets provide immediate productivity but are lower priority than schema-aware features which require more complex implementation.

**Independent Test**: Can be tested by typing partial keywords and snippet shortcuts, verifying suggestions appear, and confirming insertion works correctly.

**Acceptance Scenarios**:

1. **Given** user types `SEL`, **When** autocomplete triggers, **Then** `SELECT` keyword appears in suggestions.
2. **Given** user types a snippet shortcut like `selj`, **When** autocomplete triggers, **Then** a SELECT...JOIN template snippet appears.
3. **Given** user selects a snippet, **When** they press Tab, **Then** the snippet expands with cursor positioned at the first placeholder.
4. **Given** a snippet with multiple placeholders, **When** user presses Tab after filling one, **Then** cursor moves to the next placeholder.

---

### User Story 6 - Basic SQL Formatting (Priority: P2)

A SQL developer wants to format messy or inconsistently styled SQL code with a single command. The system provides "Format Document" and "Format Selection" commands that apply a consistent style: uppercase keywords, proper indentation, and logical line breaks.

**Why this priority**: Formatting is a high-value productivity feature that complements autocomplete. Including a basic formatter in MVP differentiates from native SSMS and matches competitor offerings.

**Independent Test**: Can be tested by pasting unformatted SQL, running Format Document, and verifying output matches expected style.

**Acceptance Scenarios**:

1. **Given** a query with lowercase keywords and no indentation, **When** user runs Format Document, **Then** keywords are uppercased and proper indentation is applied.
2. **Given** user selects a portion of a query, **When** user runs Format Selection, **Then** only the selected portion is formatted.
3. **Given** a nested subquery, **When** Format Document runs, **Then** subqueries are indented relative to their parent.
4. **Given** a JOIN clause, **When** formatted, **Then** each JOIN appears on a new line with ON predicates indented.

---

### User Story 7 - Custom Snippet Management (Priority: P3)

A SQL developer wants to create, edit, and manage their own code snippets beyond the built-in set. The system provides a settings UI where users can add custom snippets with shortcuts, placeholders, and categories.

**Why this priority**: Custom snippets are valuable for power users but less critical than core autocomplete and built-in snippets. This extends the snippet system without blocking MVP delivery.

**Independent Test**: Can be tested by opening snippet settings, creating a new snippet, then using the shortcut to verify it appears and expands correctly.

**Acceptance Scenarios**:

1. **Given** user opens SQL Essentials settings, **When** they navigate to Snippets, **Then** a list of existing snippets is displayed.
2. **Given** user clicks Add Snippet, **When** they fill in shortcut, body, and placeholders, **Then** the new snippet is saved.
3. **Given** a custom snippet exists, **When** user types its shortcut and triggers autocomplete, **Then** the custom snippet appears in suggestions.
4. **Given** user edits an existing snippet, **When** they save, **Then** subsequent uses reflect the updated content.

---

### User Story 5 - Schema Metadata Caching (Priority: P1)

The system maintains an in-memory cache of database schema metadata (tables, views, columns, foreign keys, procedures) to enable fast autocomplete without blocking the UI. The cache refreshes automatically on a configurable interval and can be manually refreshed by the user.

**Why this priority**: All autocomplete features depend on having schema metadata available. Without efficient caching, every suggestion would require a database round-trip, making the tool unusable. This is foundational infrastructure.

**Independent Test**: Can be tested by connecting to a database, verifying metadata loads in background, checking autocomplete works immediately after, and confirming manual refresh updates the cache.

**Acceptance Scenarios**:

1. **Given** user connects to a database, **When** connection is established, **Then** schema metadata begins loading asynchronously without blocking SSMS.
2. **Given** metadata is cached, **When** user triggers autocomplete, **Then** suggestions appear within 100ms using cached data.
3. **Given** user clicks "Refresh Schema", **When** refresh completes, **Then** new/modified objects appear in suggestions.
4. **Given** cache TTL expires (default 30 minutes), **When** next autocomplete triggers, **Then** cache refreshes in background while serving stale data.

---

### Edge Cases

- What happens when the database connection is lost mid-query?
  - System continues serving cached suggestions with a visual indicator that cache may be stale.
- What happens when a table has 500+ columns?
  - System uses virtualized rendering and loads columns in batches to maintain UI responsiveness.
- What happens when user types faster than suggestions can compute?
  - System debounces input (30-80ms) and cancels in-flight requests when new input arrives.
- What happens when a query has syntax errors?
  - System falls back to lexical analysis to provide best-effort suggestions based on token context.
- What happens when schema contains objects with special characters?
  - System properly escapes/brackets identifiers (e.g., `[Order Details]`).

## Requirements *(mandatory)*

### Functional Requirements

**Autocomplete Core**

- **FR-001**: System MUST display autocomplete suggestions within 100ms of trigger keystroke.
- **FR-002**: System MUST trigger autocomplete on: dot after identifier, space after keywords (SELECT/FROM/JOIN/WHERE), and configurable keyboard shortcut.
- **FR-003**: System MUST filter suggestions by prefix as user types additional characters.
- **FR-004**: System MUST allow navigation of suggestions via arrow keys and selection via Tab or Enter.
- **FR-005**: System MUST dismiss suggestions when user presses Escape or clicks outside the popup.

**Context Awareness**

- **FR-006**: System MUST detect the current SQL clause (SELECT, FROM, WHERE, JOIN, ON, GROUP BY, ORDER BY, etc.) and prioritize suggestions accordingly.
- **FR-007**: System MUST track table aliases defined in the current query and resolve `alias.` references to the correct table.
- **FR-008**: System MUST suggest only columns from the aliased table when user types `alias.`.

**JOIN Assistance**

- **FR-009**: System MUST suggest JOIN predicates based on foreign key relationships when available.
- **FR-010**: System MUST fall back to column name matching when no FK relationship exists.
- **FR-011**: System MUST rank FK-based suggestions higher than heuristic matches.

**Metadata Management**

- **FR-012**: System MUST cache schema metadata (tables, views, columns, procedures, functions, foreign keys) per database connection.
- **FR-013**: System MUST load metadata asynchronously without blocking the SSMS UI thread.
- **FR-014**: System MUST provide a manual refresh command for schema cache.
- **FR-015**: System MUST support configurable cache TTL (default: 30 minutes).

**Snippets**

- **FR-016**: System MUST provide built-in snippets for common patterns (SELECT, INSERT, UPDATE, DELETE, CTE, TRY/CATCH).
- **FR-017**: System MUST support placeholder navigation via Tab key within inserted snippets.
- **FR-018**: System MUST provide a settings UI to view, add, edit, and delete custom snippets.
- **FR-019**: Custom snippets MUST support: shortcut trigger, body text, and placeholder definitions.

**Formatting**

- **FR-020**: System MUST provide a "Format Document" command accessible via menu and keyboard shortcut.
- **FR-021**: System MUST provide a "Format Selection" command for partial formatting.
- **FR-022**: Default formatting profile MUST apply: uppercase keywords, consistent indentation (configurable spaces/tabs), JOINs on new lines, ON predicates indented.
- **FR-023**: Formatting MUST preserve semantic meaning (no changes to query logic).

**Ranking**

- **FR-024**: System MUST rank suggestions using: exact prefix match, type relevance to clause, key column status (PK/FK), and usage frequency.

### Key Entities

- **DatabaseConnection**: Represents an active connection to a SQL Server instance; contains server name, database name, authentication state.
- **SchemaCache**: Contains cached metadata for a specific database; includes tables, views, columns, foreign keys, procedures, functions; tracks last refresh time.
- **TableDefinition**: Represents a table or view; contains schema name, object name, list of columns.
- **ColumnDefinition**: Represents a column; contains name, data type, nullability, PK/FK flags.
- **ForeignKeyRelationship**: Represents a FK constraint; links parent table/column to referenced table/column.
- **Suggestion**: Represents an autocomplete candidate; contains display text, insertion text, type, relevance score.
- **Snippet**: Represents a code template; contains shortcut, expansion text, placeholder definitions.
- **AutocompleteContext**: Represents the current query state; contains cursor position, current clause, defined aliases, partial input.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Autocomplete popup appears within 100ms of trigger keystroke in 95% of cases (measured across 1000 triggers on a database with 500+ tables).
- **SC-002**: Column suggestions after `alias.` show only columns from the correct table in 100% of cases where the alias is unambiguously defined.
- **SC-003**: JOIN predicate suggestions match the FK relationship in 85% of cases where a single FK path exists between the two tables.
- **SC-004**: Schema metadata for a database with 500 tables loads completely within 2 seconds without any UI freeze.
- **SC-005**: Memory usage for cached schema metadata remains under 50MB for a database with 1000 tables and 10,000 columns.
- **SC-006**: Users can complete a typical SELECT...JOIN query with 50% fewer keystrokes compared to native SSMS IntelliSense (measured via keystroke logging in user testing).
- **SC-007**: Zero unhandled exceptions or SSMS crashes during 8-hour continuous usage sessions.

### Non-Functional Requirements

**Observability**

- **NFR-001**: System MUST collect anonymous telemetry including crash reports and performance metrics (completion latency, cache load times).
- **NFR-002**: System MUST NOT collect query text, table names, or any user data in telemetry.
- **NFR-003**: System MUST provide an opt-out setting for all telemetry collection.
- **NFR-004**: System MUST write diagnostic logs to `%APPDATA%\SqlEssentials\logs\` for local troubleshooting.

**Settings & Configuration**

- **NFR-005**: System MUST store all user settings in `%APPDATA%\SqlEssentials\` (roaming profile).
- **NFR-006**: Settings MUST survive extension reinstalls and SSMS upgrades.
- **NFR-007**: Settings files MUST be human-readable JSON format.

**Keyboard & Accessibility**

- **NFR-008**: System MUST use SSMS-native keyboard shortcuts by default (Ctrl+Space for autocomplete, Ctrl+K+D for format).
- **NFR-009**: System MUST allow users to rebind all keyboard shortcuts in settings.
- **NFR-010**: System MUST detect and warn about shortcut conflicts with other extensions.

## Assumptions

- SSMS 2016 or later is the target environment (uses VS Shell extensibility model).
- Users have read access to `sys.tables`, `sys.columns`, `sys.foreign_keys`, and related system views.
- Metadata caching is scoped per-database; switching databases triggers a new cache load.
- Initial release targets single-query-window context; cross-window alias tracking is out of scope.
- Distribution via direct VSIX download (GitHub Releases); VS Marketplace publishing deferred post-MVP.
- Licensed under MIT; no activation, license keys, or feature gating required.

## Clarifications

### Session 2026-02-04

- Q: What telemetry/error reporting approach should the extension use? → A: Anonymous telemetry — Collect crash reports + performance metrics (no query content), opt-out available.
- Q: How will the extension be distributed? → A: Direct download — Host VSIX on project website/GitHub releases; manual updates for MVP.
- Q: Where should settings be persisted? → A: Per-user roaming — Store in %APPDATA%\SqlEssentials\; survives reinstalls, can roam across machines.
- Q: What licensing model should be used? → A: Free / Open Source — MIT license; all MVP features free; no activation required.
- Q: How should keyboard shortcut conflicts be handled? → A: SSMS-native bindings — Match SSMS IntelliSense shortcuts (Ctrl+Space, Ctrl+K+D); override if conflict detected.

## Out of Scope (Future Phases)

The following features are planned for future phases and are explicitly **NOT** part of this MVP:

| Feature | Target Phase |
|---------|-------------|
| Advanced formatting profiles & CLI tool | Phase 2 |
| Refactoring: rename alias, expand `SELECT *`, qualify object names | Phase 3 |
| Code analyzer with 10-15 rules and diagnostics panel | Phase 4 |
| AI features: NL→SQL, Explain SQL, AI completions | Phase 5 (Optional) |
| Environment-based tab coloring | Future |
| Persistent query history | Future |
| Tab restoration after crash | Future |
| Team snippet sharing (file/git-based) | Future |
