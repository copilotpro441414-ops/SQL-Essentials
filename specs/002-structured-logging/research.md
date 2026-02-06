# Research: Structured Diagnostic Logging

**Phase**: 0 (Outline & Research)  
**Date**: 2026-02-06  
**Status**: Complete

## Overview

This document captures the key technical decisions for structured diagnostic logging and their rationale.

---

## 1. Log File Format

**Decision**: Use JSON Lines (one JSON object per line).

**Rationale**: JSON Lines keeps logs structured for parsing while remaining readable and append-friendly.

**Alternatives Considered**:
| Option | Rejected Because |
|--------|------------------|
| Plain text key=value | Harder to parse reliably at scale |
| CSV columns | Poor fit for nested properties and exceptions |
| Multiline human format | Not machine-friendly; harder to index |

---

## 2. Log Level Configuration

**Decision**: Read `SQL_ESSENTIALS_LOG_LEVEL` at package startup.

**Rationale**: Environment variables are frictionless for support and do not require VS settings.

**Alternatives Considered**:
| Option | Rejected Because |
|--------|------------------|
| VS options page | Adds UI work and requires UI access |
| Config file in %TEMP% | Extra I/O and discoverability issues |

---

## 3. Log Size Configuration

**Decision**: Read `SQL_ESSENTIALS_LOG_MAX_MB` (integer MB) at startup, default 10.

**Rationale**: Keeps configuration consistent with log level and allows easy tuning.

**Alternatives Considered**:
| Option | Rejected Because |
|--------|------------------|
| Hardcoded max only | No user control |
| Registry setting | Overkill and less portable |

---

## 4. Async Write Strategy

**Decision**: Use a background writer with a bounded queue; drop entries if full.

**Rationale**: Guarantees no UI blocking and caps memory usage under heavy logging.

**Alternatives Considered**:
| Option | Rejected Because |
|--------|------------------|
| Synchronous writes | Blocks UI and completion pipeline |
| Unbounded queue | Memory growth under high load |

---

## 5. Rotation Strategy

**Decision**: When size exceeds max, rename current file to `.1` and start a new file.

**Rationale**: Keeps a single backup for recent history while avoiding disk growth.

**Alternatives Considered**:
| Option | Rejected Because |
|--------|------------------|
| Truncate in place | Loses immediate history |
| Multi-file timestamp rotation | More complex and unbounded |

---

## 6. Correlation ID Policy

**Decision**: Include a correlation ID on every log entry.

**Rationale**: Ensures each log line can be attributed to a request even when interleaved.

**Alternatives Considered**:
| Option | Rejected Because |
|--------|------------------|
| Optional correlation ID | Difficult to group entries consistently |

---

## 7. Log File Location

**Decision**: Write to `%TEMP%\SqlEssentials.<PID>.debug.log`.

**Rationale**: `%TEMP%` is per-user and writable; PID avoids cross-process contention.

**Alternatives Considered**:
| Option | Rejected Because |
|--------|------------------|
| %APPDATA% | Higher persistence than necessary for diagnostics |
| Shared file without PID | Risk of contention across VS instances |

---

## 8. Exception Handling

**Decision**: Include exception type, message, and stack trace on Error entries.

**Rationale**: Supports diagnosis without attaching a debugger.

**Alternatives Considered**:
| Option | Rejected Because |
|--------|------------------|
| Message only | Insufficient detail for triage |
