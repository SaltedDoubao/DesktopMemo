# FAQ

## 1. The "modified date" shown in the list is inaccurate / refreshed after startup?

The application may perform data migration or index rebuilding on startup (e.g., migrating old format indexes to SQLite). During certain migration processes, file timestamps may be updated to ensure index consistency, causing what appears to be a "refresh."

Recommended troubleshooting steps:

1. Check recent logs in `.memodata/.logs/` for "Migration" related records
2. If you upgraded from an old version, first confirm whether index migration occurred
3. If the issue persists, report it in Issues with logs (be sure to redact sensitive information)

## 2. UI/text is hard to read (pure white background, low contrast)

Try first:

- Adjust system theme (light/dark)
- Adjust application transparency
- Choose a desktop wallpaper with higher contrast

If you'd like the application to support "automatic contrast optimization," please describe your scenario and expected behavior in Issues to help evaluate implementation approaches.

## 3. Application fails to start or crashes unexpectedly

General troubleshooting steps:

1. First, backup `.memodata`
2. Check error logs in `.memodata/.logs/`
3. If data corruption prevents startup, try temporarily moving `.memodata` (let the app regenerate) to confirm if the issue is data-related

## 4. How can I pin multiple independent memo windows side by side?

This is a common request and currently under planning/discussion. You can describe your use case in Issues (e.g., multi-window independent pinning/position memory/independent transparency) to help break down requirements and estimate effort.

---

**Last Updated**: 2025-12-26
