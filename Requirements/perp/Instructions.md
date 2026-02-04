# SQL Essentials – Development Kickstart Prompt

This prompt is written for an AI or human developer who will implement the **SQL Essentials** SSMS plugin.

- You will implement SQL Essentials in **C#** as a **VSIX / VSPackage** extension for SSMS. [web:24][web:53][web:56]
- Your main goal in Phase 1 is a **fast, smart autocomplete** + **JOIN helper**, similar to SQL Prompt and dbForge SQL Complete. [web:16][web:39][web:51][web:58]

You should read together with:

- `sql_essentials_research.md` – requirements & competitive analysis.
- `sql_essentials_technical_spec.md` – architecture & data structures.

---

## 1. Product Goal & Success Criteria

### 1.1 Product Goal

Build an SSMS extension that:

- Significantly speeds up writing T‑SQL.
- Provides context‑aware IntelliSense and JOIN completion.
- Offers snippets and basic formatting.
- Feels as responsive as commercial tools. [web:51][web:39][web:58]

### 1.2 Success Criteria

- Autocomplete appears within **≤100 ms** after trigger in normal use.
- JOIN suggestions correctly propose FK‑based conditions in **≥85%** of typical schemas. [web:16][web:39]
- No crashes or unhandled exceptions over prolonged sessions.
- Users can comfortably use keyboard only (no mouse required).
- Works in SSMS 2016+ and with SQL Server 2012+.

---

## 2. High‑Level Plan (Weeks)

You will develop in **incremental weekly milestones**:

1. Week 1 – Project skeleton, extension loading, editor access.
2. Week 2 – Lexer and parser (enough for context analysis).
3. Week 3 – Context analyzer and suggestion engine.
4. Week 4 – Popup UI and end‑to‑end autocomplete.
5. Week 5 – JOIN assistance (ON clause suggestions).

Phase 2 and beyond can then add refactoring, formatting, analysis.

---

## 3. Week 1 – Foundation & Infrastructure

### 3.1 Task: Create VSIX SSMS Extension Project

Using guidance for SSMS 18/2016 extensions: [web:24][web:53][web:56]

**Steps:**

1. Install Visual Studio with **Visual Studio extension development** workload. [web:53]
2. Create project:  
   `File → New Project → Extensibility → VSIX Project`. [web:24][web:53]
3. Add a **VSPackage**:  
   `Right click project → Add → New Item → Extensibility → Visual Studio Package`. [web:24]
4. Configure debugging:
   - Start external program: `Ssms.exe` path for target SSMS version.
   - Ensure VSIX deploys content to SSMS Extensions folder, as described in community examples. [web:53][web:56]

**Deliverables:**

- A VSIX project that, when built and debugged, launches SSMS and loads the package (even if it just adds a dummy menu).

**Acceptance:**

- A menu item under `Tools` (e.g., “SQL Essentials Settings…”) appears.
- Clicking it shows a simple message box.

---

### 3.2 Task: Dependency Injection Setup

You need a lightweight DI layer to keep architecture clean.

**Steps:**

- Add `Microsoft.Extensions.DependencyInjection`.
- Create a `ServiceCollection` in the package initialization:

```csharp
var services = new ServiceCollection();

services.AddSingleton<ISqlLexer, SqlLexer>();
services.AddSingleton<ISqlScriptParser, SqlScriptParser>();
services.AddSingleton<IContextAnalyzer, ContextAnalyzer>();
services.AddSingleton<ISuggestionGenerator, SuggestionGenerator>();
services.AddSingleton<IDatabaseSchemaRepository, DatabaseSchemaRepository>();
services.AddSingleton<IMetadataCache, MetadataCache>();
services.AddSingleton<IUserSettingsRepository, UserSettingsRepository>();
services.AddSingleton<ISnippetRepository, SnippetRepository>();
// ... others as needed

_serviceProvider = services.BuildServiceProvider();
