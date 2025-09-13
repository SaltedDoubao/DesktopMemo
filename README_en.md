<h1 align="center">DesktopMemo<h1>

> <p align="center">A simple Windows desktop memo application</p>

<div align="center">

<img src="https://img.shields.io/badge/.NET-8.0-purple" />
<img src="https://img.shields.io/badge/Platform-Windows-blue" />
<img src="https://img.shields.io/badge/License-MIT-green" />

[中文 README](./README.md) | English README

</div>

## 📋 Project Overview

DesktopMemo is a desktop memo application developed with WPF technology, providing rich window management features and memo management capabilities. Whether for daily notes, work reminders, or inspiration recording, it offers convenient solutions for your needs.

## ✨ Key Features

### 🏠 Window Management
- **Three topmost modes**: Normal mode, pinned to desktop, pinned on top of windows
- **Position memory**: Automatically saves and restores window position
- **Click through**: When enabled, allows clicking through to the desktop without affecting your normal work
- **Window lock**: Prevents accidental window dragging

### 🎨 User Experience
- **Modern interface**: Clean and beautiful user interface
- **Quick operations**: Rich keyboard shortcut support
- **Status indicators**: Real-time status information display
- **Balloon tips**: Friendly notifications for important operations

## 🖥️ System Requirements

- **Operating System**: Windows 10 version 1809 or higher
- **Architecture**: x64
- **.NET Runtime**: No separate installation required (packaged in executable)
- **Memory**: Recommended 512MB available memory
- **Storage**: Approximately 200MB disk space

## 🚀 Quick Start

### Download & Installation

1. Download the latest version of `DesktopMemo.exe` from the [Releases](../../releases) page
2. Save the application to a location you can find
3. Double-click to run and start using

> **Note**: When running for the first time, Windows Defender may perform a security scan or even report false positives, which is normal behavior.

### Basic Usage

1. **Create memo**: Click the `+` button or use the tray menu "New Memo"
2. **Edit content**: Type directly in the text area, first line is the title, second line onwards is the content
3. **Switch memos**: Use the memo list or shortcuts to switch between memos
4. **Adjust window**: Drag window edges to resize, drag title bar to move position

## 🛠️ Development Information

### Tech Stack

- **Framework**: .NET 8.0 + WPF
- **Language**: C# 12.0
- **UI**: XAML + Code-behind
- **Data Storage**: JSON
- **Package Management**: NuGet

### Project Structure

```
DesktopMemo/
├── App.xaml              # Application entry
├── App.xaml.cs           # Application logic
├── MainWindow.xaml       # Main window interface
├── MainWindow.xaml.cs    # Main window logic
├── AssemblyInfo.cs       # Assembly information
├── DesktopMemo.csproj    # Project file
├── DesktopMemo.sln       # Solution file
└── README.md             # Documentation
```

### Building the Project

```bash
# Clone the project
git clone https://github.com/SaltedDoubao/DesktopMemo.git
cd DesktopMemo

# Restore dependencies
dotnet restore

# Build project
dotnet build --configuration Release

# Run project
dotnet run

# Publish single-file version
dotnet publish --configuration Release --output ./publish
```

## 📄 Data Storage

Application data is saved in the following location:

```
%APPDATA%\DesktopMemo\
├── settings.json         # Application settings
└── Data\
    └── memos.json        # Memo data
```

## 🔧 Configuration Files

### settings.json Structure

```json
{
  "SavedX": 100.0,
  "SavedY": 100.0,
  "PositionRemembered": false,
  "AutoRestorePositionEnabled": true,
  "TopmostMode": "Desktop",
  "ClickThroughEnabled": false,
  "AutoStartEnabled": false,
  "NoteContent": "",
  "ShowExitPrompt": true
}
```

## 🤝 Contributing

Contributions are welcome! Please follow these steps:

1. Fork the project
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Create a Pull Request

## 📝 Changelog

See [Releases](../../releases) for details

## 🚧 Development Roadmap

- [ ] Modify text storage method
- [ ] Add multi-language UI
- [ ] Add shortcut key functionality
- [ ] Performance optimization

## 🐛 Bug Reports

Encountered an issue? Please report it through:

1. [GitHub Issues](../../issues) - Submit bug reports or feature requests
2. Provide detailed steps to reproduce the issue
3. Include system environment information and error screenshots

## 📄 License

This project is licensed under the [MIT License](LICENSE) - see the LICENSE file for details

## 🙏 Acknowledgments

Thanks to all developers and users who have contributed to this project!

---

<div align="center">

**If this project helped you, please consider giving it a ⭐!**

[Report Bug](../../issues) • [Request Feature](../../issues) • [Contribute](../../pulls)

</div>
