# Data Migration

DesktopMemo performs necessary data migrations on startup to ensure data structure consistency across versions.

## Migration Scenario Examples

- Todo: Migrate from old formats (e.g., JSON) to SQLite (`todos.db`)
- Memo index: Migrate from old index files (e.g., `index.json`) to SQLite (`memos.db`)

## Migration Observable Signals

- `.memodata/.logs/` contains `Migration` related records
- Database files are updated after migration (e.g., `memos.db`, `todos.db`)

## Notes

- If migration involves file index rebuilding, some file timestamps may be updated to ensure index consistency
- For migration issues, prioritize checking logs and include redacted key information when submitting Issues

---

**Last Updated**: 2025-12-26
