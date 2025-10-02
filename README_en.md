<h1 align="center">SaltedDoubao's Desktop Memo</h1>

> <p align="center">A brand new desktop memo application with MVVM architecture and Markdown storage</p>

<div align="center">

<img src="https://img.shields.io/badge/.NET-9.0-purple" />
<img src="https://img.shields.io/badge/Platform-Windows-blue" />
<img src="https://img.shields.io/badge/License-MIT-green" />

[中文 README](README.md) | English README

</div>

## 📋 Project Overview

DesktopMemo is an application based on .NET 9.0 + WPF + CommunityToolkit.Mvvm, providing rich window management features and memo management capabilities. Whether it's daily notes, work memos, or inspiration records, it provides you with a convenient solution.

## ✨ Key Features

### 📝 Memo Management
- Quickly create and edit Markdown-formatted memos
- Auto-save, never lose your notes
- List view for quick preview

### ✅ Todo List
- Lightweight todo list management
- Quick add, check, and delete todo items
- Completed items shown with strikethrough, can undo or clear
- Click "Memo/Todo List" text in the title bar to switch modes

### 🪟 Window Management
- Three stay-on-top modes (Normal, Desktop, Always on Top)
- Adjustable transparency and click-through mode
- Flexible window position presets

## 🖥️ System Requirements

- **Operating System**: Windows 10 version 1903 and above (WPF dependency)
- **Architecture**: x86_64
- **.NET SDK**: 9.0 (for development/debugging)
- **.NET runtime**: Built into the release package, no additional installation required

## 🚀 Quick Start

### Get Source Code

```bash
# Clone repository
git clone https://github.com/SaltedDoubao/DesktopMemo.git
cd DesktopMemo
```

### Build and Run

```powershell
# Restore dependencies
dotnet restore DesktopMemo.sln

# Debug build
dotnet build DesktopMemo.sln --configuration Debug

# Run application
dotnet run --project src/DesktopMemo.App/DesktopMemo.App.csproj --configuration Debug
```

> To build an executable file, you can use `build_exe.bat` in the repository root. Build artifacts are output to `artifacts/v<version>/bin` by default. You can customize the version number by setting the environment variable `ArtifactsVersion`.

On first run, `/.memodata` will be generated in the executable directory:

```
.memodata
├── content/
│   ├── index.json           # Memo index
│   └── {memoId}.md          # YAML Front Matter + content
└── settings.json            # Window and global settings
```

### Shortcuts

> Global
- Ctrl+N - Quickly create new memo
> Only available on edit page
- Tab - Insert indentation (4 spaces)
- Shift+Tab - Decrease indentation
- Ctrl + S - Save current memo (seems unnecessary)
- Ctrl + Tab / Ctrl + Shift + Tab - Switch memos
- Ctrl + F / Ctrl+H - Find/Replace
- F3 / Shift + F3 - Find next/previous
- Ctrl + ] / Ctrl + [ - Increase/decrease indentation
- Ctrl + D - Duplicate current line downward

### Importing Old Data

Data from versions before v2.0.0 is not compatible with the new version and can be imported as follows:

**Automatic Import** (Recommended):
1. Ensure the old application data directory (`Data/content/`) is in the application directory
2. The program will automatically import old data on first run
3. Manually delete the Data directory after import is complete
4. Done!

**Manual Import**:
1. Copy old memo files (`Data/content/*.md`) to the new application data directory (`.memodata/content/`)
2. Click "Import Old Data" in the application settings
3. Done!

## 🧭 Project Structure

```
DesktopMemo_rebuild/
├── DesktopMemo.sln
├── build_exe.bat                   # Build executable script
├── docs/                           # Project documentation
├── src/                            # Project main directory
├── artifacts/                      # Build output directory
└── publish/                        # Release artifacts
```

## 🛠️ Tech Stack

- **Framework**: .NET 9.0, WPF
- **Pattern**: MVVM (based on CommunityToolkit.Mvvm)
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection
- **Persistence**: Markdown + YAML Front Matter; Window settings use JSON
- **Toolchain**: PowerShell build scripts, GitHub Actions (planned)

## 🤝 Contributing

Welcome to collaborate through Issues or PRs:

1. Fork this repository
2. Create feature branch: `git checkout -b your-feature`
3. Commit changes: `git commit -m "commit message"`
4. Push branch: `git push origin your-feature`
5. Create Pull Request with requirements background, change summary and verification results

If you haven't changed the version number, you **must** clear the `artifacts/vX.X.X (corresponding version number)` directory before building the project using `dotnet build`.

It's recommended to run `dotnet build` / `dotnet run` before submitting to ensure basic validation passes.

For other development considerations, see [应用开发规范.md](docs/应用开发规范.md)

## 📝 Changelog

### v2.2.0 (Latest)
- ✨ Added multi-language support
  - Currently supported languages: 
    - Simplified Chinese
    - Traditional Chinese
    - English

For more history, see [Releases](../../releases)

## 🚧 Roadmap

### Updates that may be implemented soon
- [ ] Add preset window size schemes
- [ ] Add theme switching functionality
- [x] ~~Multi-language support~~ ✅ Implemented (v2.2.0)
- [x] ~~Todo-list functionality~~ ✅ Implemented (v2.1.0)

### Future update trends
- To be added...

## 📄 License

This project is licensed under the [MIT License](LICENSE).

---

<div align="center">

**If this project is helpful to you, please consider giving it a ⭐!**\
**Your ⭐ is the greatest motivation for my updates!**

[Report Bug](../../issues) • [Request Feature](../../issues) • [Contribute Code](../../pulls)

</div>
