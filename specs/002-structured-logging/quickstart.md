# Quickstart: Structured Diagnostic Logging

**Phase**: 1 (Design & Contracts)  
**Date**: 2026-02-06  
**Target**: Windows 10+ with Visual Studio 2022 / SSMS 18+

## Enable Logging

1. Set the log level environment variable:

```powershell
setx SQL_ESSENTIALS_LOG_LEVEL Debug
```

2. (Optional) Set the max log size in MB:

```powershell
setx SQL_ESSENTIALS_LOG_MAX_MB 10
```

3. Restart Visual Studio or SSMS to apply the environment variables.

---

## Log File Location

Logs are written to:

```
%TEMP%\SqlEssentials.<PID>.debug.log
```

Each VS/SSMS process writes its own file using the current process ID.

---

## Sample Log Entry (JSON Lines)

```json
{"timestamp":"2026-02-06T14:23:17.412Z","level":"Info","component":"Package","message":"Initialized","correlationId":"req-1f7a3d","properties":{"version":"1.0.0"}}
```

---

## Rotation Behavior

- When the log exceeds the max size, the current file is renamed to `.1`.
- A new log file starts immediately with the original name.
- Only one backup file is kept.

---

## Disable Logging

Unset the environment variable and restart VS/SSMS:

```powershell
setx SQL_ESSENTIALS_LOG_LEVEL ""
```

When the variable is unset or set to `Off`, logging is disabled and no file is created.
