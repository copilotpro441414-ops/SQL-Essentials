# Data Model: Structured Diagnostic Logging

**Phase**: 1 (Design & Contracts)  
**Date**: 2026-02-06  
**Source**: [spec.md](./spec.md) Key Entities section

## Overview

This document defines the core entities used by structured diagnostic logging.

---

## 1. LogLevel

Controls verbosity for log filtering.

```csharp
public enum LogLevel
{
    Off = 0,
    Error = 1,
    Warning = 2,
    Info = 3,
    Debug = 4,
    Trace = 5
}
```

**Validation Rules**:
- Values outside the range are invalid and treated as `Off`.

---

## 2. LogEntry

Represents a single log record written to the JSON Lines file.

```csharp
public sealed class LogEntry
{
    public DateTimeOffset Timestamp { get; }
    public LogLevel Level { get; }
    public string Component { get; }
    public string Message { get; }
    public string CorrelationId { get; }
    public IReadOnlyDictionary<string, object> Properties { get; }
    public LogExceptionDetails Exception { get; }
}

public sealed class LogExceptionDetails
{
    public string Type { get; }
    public string Message { get; }
    public string StackTrace { get; }
}
```

**Validation Rules**:
- `Timestamp` must be ISO 8601 when serialized.
- `Component`, `Message`, and `CorrelationId` must be non-empty.
- `Properties` may be empty but not null (use empty dictionary).
- `Exception` is null unless level is `Error` and an exception is provided.

---

## 3. LogScope

Tracks elapsed time and correlation ID for a sequence of operations.

```csharp
public sealed class LogScope : IDisposable
{
    public string CorrelationId { get; }
    public string Component { get; }
    public string Operation { get; }
    public DateTimeOffset StartTime { get; }
    public TimeSpan Elapsed { get; }

    public void Dispose(); // Emits timing entry
}
```

**State Transitions**:
```
Created -> Disposed
```

---

## 4. LogFileOptions

Captures file path and size/rotation settings.

```csharp
public sealed class LogFileOptions
{
    public string LogFilePath { get; }
    public int MaxFileSizeMb { get; }
    public int MaxFileSizeBytes => MaxFileSizeMb * 1024 * 1024;
}
```

**Validation Rules**:
- `MaxFileSizeMb` must be >= 1; default to 10 when missing or invalid.
- `LogFilePath` must include the process ID suffix.
