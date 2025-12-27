# Data & Backup

## Where is the Data Directory?

DesktopMemo creates a `/.memodata` directory by default in the executable's directory:

```
.memodata/
├── content/                 # Markdown memo files (YAML Front Matter + content)
├── .logs/                   # Log files directory
├── settings.json            # Window and global settings
├── memos.db                 # Memos database (v2.3.0+)
└── todos.db                 # Todo items database (v2.3.0+)
```

> Different versions may contain migration files or compatibility data (e.g., the earlier `index.json`).

## Backup Recommendations

### Minimal Backup

- `settings.json`
- `memos.db`, `todos.db`
- `content/` (if you want to preserve Markdown content and metadata)

### Recommended Backup Strategy

- Exit the application before backing up to avoid inconsistent file states during writes
- Regularly package and backup the entire `.memodata` directory
- Recommended to keep at least 3 historical backups

## Migration & Consistency

The application may perform migration tasks on startup (e.g., migrating old indexes to SQLite). During migration, file timestamps may be updated to ensure index-file consistency; refer to [FAQ](05_faq.md) if you have questions.

## Data Recovery (General Approach)

1. Exit DesktopMemo
2. Overwrite the backed-up `.memodata` to the current version's executable directory
3. Restart the application

---

**Last Updated**: 2025-12-26
