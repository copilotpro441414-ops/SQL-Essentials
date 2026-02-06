# Feature Specification: Structured Diagnostic Logging

**Feature Branch**: `002-structured-logging`  
**Created**: 2026-02-06  
**Status**: Draft  
**Input**: User description: "Add detailed debugging statements via a structured logging component to trace all interactions inside the extension"

## Clarifications

### Session 2026-02-06

- Q: What log file format should be used for entries? → A: JSON Lines (one JSON object per line)
- Q: How should the log max size be configured? → A: Environment variable (e.g., SQL_ESSENTIALS_LOG_MAX_MB)
- Q: How should log write buffering be handled under load? → A: Background writer with bounded queue (drop if full)
- Q: What rotation strategy should be used when the max size is reached? → A: Rename current to .1 and start new (keep one backup)
- Q: Should correlation ID be included in every log entry? → A: Yes

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Diagnose Extension Behavior via Log File (Priority: P1)

A developer or support engineer needs to understand what the SQL Essentials extension is doing at runtime — which components initialized, what completion requests were made, how the cache behaved, what context was detected, and whether errors occurred. They enable diagnostic logging and open a single log file that gives a complete chronological trace of all extension activity.

**Why this priority**: Without observable behavior, debugging extension issues (e.g., "why aren't suggestions appearing?") requires guesswork. A log file is the single most valuable diagnostic tool.

**Independent Test**: Enable logging, open a SQL file, type a query, close the file. Open the log file and verify it contains timestamped entries covering package initialization, content type detection, completion trigger, context analysis, cache lookup, suggestion generation, and elapsed times.

**Acceptance Scenarios**:

1. **Given** logging is enabled, **When** the extension package initializes, **Then** the log file contains an entry with timestamp, component name "Package", and message "Initialized" with version info
2. **Given** logging is enabled, **When** the user types `SELECT` in a SQL editor, **Then** the log file contains entries for: trigger detection, context analysis (clause=Select), cache lookup, suggestion count, and elapsed time
3. **Given** logging is enabled, **When** a completion request fails, **Then** the log file contains an Error-level entry with the exception type, message, and stack trace
4. **Given** logging is disabled (default), **When** the extension runs normally, **Then** no log file is created and no measurable performance impact occurs

---

### User Story 2 - Control Logging via Environment Variable (Priority: P1)

A developer controls diagnostic logging through a simple environment variable, without needing to modify code, rebuild, or access VS settings. They can set the log level to control verbosity.

**Why this priority**: Users need a zero-friction way to enable/disable logging that works across VS versions and doesn't require extension reinstallation.

**Independent Test**: Set `SQL_ESSENTIALS_LOG_LEVEL=Debug`, launch VS, perform SQL editing. Verify Debug-level entries appear. Unset the variable, restart VS, verify no log file is written.

**Acceptance Scenarios**:

1. **Given** `SQL_ESSENTIALS_LOG_LEVEL` is set to "Debug", **When** VS starts, **Then** the log file includes Debug and higher severity entries
2. **Given** `SQL_ESSENTIALS_LOG_LEVEL` is set to "Trace", **When** a completion request runs, **Then** detailed method enter/exit entries appear with parameter values
3. **Given** `SQL_ESSENTIALS_LOG_LEVEL` is not set, **When** VS starts, **Then** logging is disabled and no file I/O occurs for logging
4. **Given** `SQL_ESSENTIALS_LOG_LEVEL` is set to "Error", **When** normal operations succeed, **Then** only error entries appear in the log

---

### User Story 3 - Trace Performance Across Components (Priority: P2)

A developer investigating latency issues needs to see timing breakdowns for each stage of the completion pipeline — how long context analysis took, how long the cache lookup took, how long suggestion ranking took — all in one log file with correlated request IDs.

**Why this priority**: The existing perf.log only captures total elapsed time. Understanding which component is slow requires per-component timing.

**Independent Test**: Enable Trace-level logging, trigger a completion, open the log file. Verify each pipeline stage has its own timed entry and all entries for the same request share a correlation ID.

**Acceptance Scenarios**:

1. **Given** Trace-level logging is enabled, **When** autocomplete triggers, **Then** log entries for ContextAnalyzer, SchemaCache, CompletionEngine, and SqlCompletionSource each show elapsed milliseconds
2. **Given** multiple completions trigger rapidly, **When** reviewing the log, **Then** entries for each request are distinguishable by a unique request ID
3. **Given** a cache refresh runs in the background, **When** it completes, **Then** the log shows the refresh duration and number of objects loaded

---

### Edge Cases

- What happens when the log file grows very large? Logs are capped at a configurable maximum size (default 10 MB) and rotate to a single backup file.
- What happens if the log directory is not writable? The logger silently degrades — no exceptions propagate to the host, and logging stops for that session.
- What happens during rapid typing (many log entries per second)? Writes are buffered and flushed asynchronously to avoid blocking the UI thread.
- What happens if two VS instances write to the same log file? Each instance includes a process ID in the log file name (e.g., `SqlEssentials.12345.debug.log`) to avoid contention.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide a centralized logging interface that all components use instead of ad-hoc scattered logging calls
- **FR-002**: The system MUST support five log levels: Trace, Debug, Info, Warning, Error — where Trace is most verbose and Error is least
- **FR-003**: The system MUST write log entries to a file at `%TEMP%\SqlEssentials.<PID>.debug.log` where `<PID>` is the host process ID
- **FR-004**: Each log entry MUST include: ISO 8601 timestamp, log level, component name (source class), message, correlation ID, and optional structured properties
- **FR-004a**: Log files MUST use JSON Lines format (one JSON object per line)
- **FR-005**: The system MUST read the logging level from the `SQL_ESSENTIALS_LOG_LEVEL` environment variable at startup (values: Off, Error, Warning, Info, Debug, Trace)
- **FR-006**: When logging is Off or the environment variable is unset, the system MUST perform no file I/O and add negligible overhead (guard check only)
- **FR-007**: The system MUST buffer log writes and flush asynchronously using a bounded queue; when full, drop entries rather than blocking the UI thread or completion pipeline
- **FR-008**: The system MUST rotate log files when they exceed a maximum size (default 10 MB) by renaming the current file to `.1` and starting a new file, keeping one backup file
- **FR-008a**: The maximum log size MUST be configurable via environment variable `SQL_ESSENTIALS_LOG_MAX_MB` (integer megabytes), default 10
- **FR-009**: Error-level log entries MUST include exception type, message, and stack trace when an exception is provided
- **FR-010**: The system MUST support a correlation ID (request ID) that groups all log entries belonging to a single completion request
- **FR-011**: The system MUST provide instrumentation at these points: package initialization, connection events, cache load/refresh/expiry, context analysis start/end, completion engine start/end, suggestion count and ranking, completion source trigger and response, command execution
- **FR-012**: The system MUST replace all existing ad-hoc logging (scattered Debug.WriteLine calls, File.AppendAllText to perf.log, ActivityLog calls) with calls through the centralized logger
- **FR-013**: The system MUST flush and close the log file cleanly when the VS package disposes

### Key Entities

- **LogEntry**: A single log record — timestamp, level, component, message, correlation ID, properties dictionary, optional exception
- **LogFormat**: JSON Lines (one JSON object per line)
- **LogLevel**: Enumeration controlling verbosity — Trace (most verbose), Debug, Info, Warning, Error, Off
- **Logger**: The central logging service — accepts log entries, applies level filtering, writes to output asynchronously
- **LogScope**: A disposable scope that tracks elapsed time and correlation ID for a sequence of related operations

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: With logging disabled (default), completion latency increases by no more than 1 microsecond per request (guard-check overhead only)
- **SC-002**: With Trace-level logging enabled, completion latency increases by no more than 2 milliseconds per request
- **SC-003**: A developer can diagnose a "no suggestions appearing" issue by reading the log file alone, without attaching a debugger, within 5 minutes
- **SC-004**: Every component in the extension (Package, CompletionSource, CompletionEngine, ContextAnalyzer, SchemaCache, SmoMetadataLoader, Commands) produces at least one log entry per operation
- **SC-005**: The log file does not exceed 10 MB before rotating, preventing disk space issues during extended sessions
- **SC-006**: All existing ad-hoc logging (scattered Debug.WriteLine calls, perf.log writes, ActivityLog calls) is removed and replaced with the centralized logger

## Assumptions

- The `%TEMP%` directory is always writable for the current user session in Windows.
- Environment variables are read once at package initialization; changing the variable requires restarting VS.
- The process ID suffix in the log filename is sufficient to avoid file contention between concurrent VS instances.
- Buffered async writes are adequate for the expected log volume without needing a third-party library.
- The existing `SqlEssentials.perf.log` file will be retired in favor of the new structured log file.
