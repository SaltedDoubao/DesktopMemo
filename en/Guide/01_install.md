# Installation & Updates

## Getting the Application

Download the packaged version from GitHub Releases:

- Releases: [https://github.com/SaltedDoubao/DesktopMemo/releases](https://github.com/SaltedDoubao/DesktopMemo/releases)

> If you're a developer who needs to compile and run locally, see the [Developer Guide](../Dev/README.md).

## System Requirements

- Windows 10 version 1903 or higher (WPF dependency)
- x86_64
- Release packages typically include the .NET runtime; .NET SDK is only required when compiling from source

## Extract and Run

1. Extract to any directory (avoid paths requiring administrator privileges)
2. Double-click `DesktopMemo.exe` to run

On first launch, a `/.memodata` data directory will be created alongside the executable.

## Update Methods

### Method A: Overwrite Update (Recommended)

1. Exit DesktopMemo
2. Backup `/.memodata` (at minimum, backup `settings.json` and database files)
3. Overwrite the old version with the new version files
4. Restart the application

### Method B: Keep Multiple Versions in Parallel

If you want to keep multiple versions, ensure each version has its own separate executable directory, as data is stored by default in the same directory as `DesktopMemo.exe`.

## Version Upgrade & Migration Notes

The application may perform data migration on startup (e.g., migrating old format indexes to SQLite). If you notice phenomena like "file modification times being updated," please refer to the relevant entries in [FAQ](05_faq.md).

---

**Last Updated**: 2025-12-26
