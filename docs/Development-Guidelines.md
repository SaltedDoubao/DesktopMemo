# DesktopMemo Development Guidelines

This document is for developers contributing to DesktopMemo, helping you quickly understand the project structure, development environment, build/run workflows, and common debugging paths.

> Note: Due to the author's limited English proficiency, this document was translated with the help of Claude 4.5 Sonnet. If you spot any errors or have suggestions, please open an issue. Thank you for your understanding.

## 1. Project Overview

- **Solution**: `DesktopMemo.sln`
- **Core Components**
  - `DesktopMemo.App`: WPF frontend (UI, commands, view models).
  - `DesktopMemo.Core`: Domain models and contract interfaces (pure .NET library).
  - `DesktopMemo.Infrastructure`: File storage, settings services, and other implementations.
- **Data Storage**: Writes to `/.memodata` in the executable directory by default
  - Memos: Markdown files (with YAML Front Matter) + `index.json` index
  - Todo items: `todos.json` file
  - Application settings: `settings.json` file

## 2. Development Environment Requirements

- Windows 10 or later (requires .NET desktop runtime support).
- [.NET SDK 9.0](https://dotnet.microsoft.com/download/dotnet/9.0).
- IDE: Use your preferred IDE.

## 3. Project Structure

```
DesktopMemo_rebuild/
├── DesktopMemo.sln                 # Visual Studio solution file
├── Directory.Build.props            # Unified build properties configuration
├── build_exe.bat                    # Build script for the executable
├── LICENSE                          # Open source license
├── README.md                        # Project documentation (English)
├── README_zh.md                     # Project documentation (Chinese)
├── CLAUDE.md                        # Claude AI development guide
├── docs/                            # Project documentation
│   ├── Development-Guidelines.md   # Development guidelines (this document)
│   ├── 应用开发规范.md             # Development guidelines (Chinese)
│   ├── MySQL-Integration-Specification.md # MySQL integration specification (English)
│   └── MySQL-集成规范.md           # MySQL integration specification (Chinese)
├── src/                             # Source code directory
│   ├── DesktopMemo.App/            # WPF frontend application
│   │   ├── App.xaml(.cs)           # Application startup & DI registration
│   │   ├── MainWindow.xaml(.cs)    # Main window
│   │   ├── AssemblyInfo.cs         # Assembly information
│   │   ├── DesktopMemo.App.csproj  # Project file
│   │   ├── ViewModels/             # MVVM view models
│   │   │   ├── MainViewModel.cs    # Main view model (core logic)
│   │   │   ├── MemoListViewModel.cs # Memo list view model
│   │   │   └── TodoListViewModel.cs # Todo list view model
│   │   ├── Views/                  # View components
│   │   │   ├── ConfirmationDialog.xaml(.cs)      # Generic confirmation dialog
│   │   │   └── ExitConfirmationDialog.xaml(.cs)  # Exit confirmation dialog
│   │   ├── Converters/             # WPF value converters
│   │   │   ├── EnumToBooleanConverter.cs         # Enum to boolean converter
│   │   │   ├── InverseBooleanToVisibilityConverter.cs # Inverse boolean visibility converter
│   │   │   └── CountToVisibilityConverter.cs     # Number to visibility converter
│   │   ├── Localization/           # Multi-language localization
│   │   │   ├── LocalizationService.cs # Localization service
│   │   │   ├── LocalizeExtension.cs   # XAML localization extension
│   │   │   └── Resources/             # Resource files
│   │   │       ├── Strings.resx       # Simplified Chinese (default)
│   │   │       ├── Strings.en-US.resx # English
│   │   │       ├── Strings.zh-TW.resx # Traditional Chinese
│   │   │       ├── Strings.ja-JP.resx # Japanese
│   │   │       └── Strings.ko-KR.resx # Korean
│   │   ├── Resources/              # UI resources
│   │   │   ├── Styles.xaml         # Global styles
│   │   │   ├── GlassResources.xaml # Glass effect resources
│   │   │   └── Themes/             # Themes
│   │   │       ├── Light.xaml      # Light theme
│   │   │       └── Dark.xaml       # Dark theme
│   │   └── Services/               # Application layer services (reserved)
│   ├── DesktopMemo.Core/           # Core domain layer (pure .NET library)
│   │   ├── DesktopMemo.Core.csproj # Project file
│   │   ├── Constants/              # Constant definitions
│   │   │   └── WindowConstants.cs  # Window-related constants
│   │   ├── Contracts/              # Contract interfaces
│   │   │   ├── IMemoRepository.cs       # Memo repository interface
│   │   │   ├── ITodoRepository.cs       # Todo repository interface
│   │   │   ├── ISettingsService.cs      # Settings service interface
│   │   │   ├── IWindowService.cs        # Window service interface
│   │   │   ├── IWindowSettingsService.cs # Window settings service interface
│   │   │   ├── ITrayService.cs          # System tray service interface
│   │   │   ├── ILocalizationService.cs  # Localization service interface
│   │   │   └── IMemoSearchService.cs    # Memo search service interface
│   │   ├── Models/                 # Domain models
│   │   │   ├── Memo.cs             # Memo model
│   │   │   ├── TodoItem.cs         # Todo item model
│   │   │   ├── WindowSettings.cs   # Window settings model
│   │   │   ├── AppTheme.cs         # Application theme enum
│   │   │   └── SyncStatus.cs       # Sync status enum
│   │   └── Helpers/                # Helper utility classes
│   │       ├── TransparencyHelper.cs # Transparency calculation helper
│   │       └── DebounceHelper.cs     # Debounce helper
│   ├── DesktopMemo.Infrastructure/ # Infrastructure layer (implementation layer)
│   │   ├── DesktopMemo.Infrastructure.csproj # Project file
│   │   ├── Repositories/           # Data repository implementations
│   │   │   ├── FileMemoRepository.cs          # File-based memo storage
│   │   │   ├── SqliteIndexedMemoRepository.cs # SQLite memo storage (v2.3.0+)
│   │   │   ├── JsonTodoRepository.cs          # JSON todo storage
│   │   │   └── SqliteTodoRepository.cs        # SQLite todo storage (v2.3.0+)
│   │   └── Services/               # Service implementations
│   │       ├── JsonSettingsService.cs         # JSON settings service
│   │       ├── MemoSearchService.cs           # Memo search service
│   │       ├── WindowService.cs               # Window service (Win32 API)
│   │       ├── TrayService.cs                 # System tray service
│   │       ├── MemoMigrationService.cs        # Memo migration service
│   │       ├── MemoMetadataMigrationService.cs # Memo metadata migration
│   │       └── TodoMigrationService.cs        # Todo migration service
│   └── images/                     # Application icon resources
│       └── logo.ico                # Application icon
├── artifacts/                      # Build output directory (organized by version)
│   └── v<version>/
│       ├── bin/                    # Binary output
│       └── obj/                    # Intermediate files
└── publish/                        # Published artifacts directory
```

### Key Directories and Files

**Frontend Application Layer (DesktopMemo.App)**
- `ViewModels/MainViewModel.cs`: Core application logic, handles memo list, edit state, page switching, settings panel, theme switching, etc.
- `ViewModels/TodoListViewModel.cs`: Todo view model, handles CRUD operations and state toggling for todo items.
- `ViewModels/MemoListViewModel.cs`: Memo list view model.
- `Views/ConfirmationDialog.xaml`: Generic confirmation dialog with support for custom messages and "Don't show again" option.
- `Views/ExitConfirmationDialog.xaml`: Exit confirmation dialog.
- `Converters/CountToVisibilityConverter.cs`: Number to visibility converter for UI display control (e.g., todo count badge).
- `Localization/LocalizationService.cs`: Multi-language localization service, supports Chinese, English, Japanese, Korean, and Traditional Chinese.
- `Resources/Styles.xaml`: Global style definitions.
- `Resources/GlassResources.xaml`: Glass effect (Aero) resource dictionary.
- `Resources/Themes/Light.xaml` and `Dark.xaml`: Light and dark theme resources.

**Core Domain Layer (DesktopMemo.Core)**
- `Contracts/`: All interface definitions, implementing the Dependency Inversion Principle.
- `Models/Memo.cs`: Memo domain model, includes sync status fields.
- `Models/TodoItem.cs`: Todo item domain model.
- `Models/WindowSettings.cs`: Window settings model (position, size, transparency, etc.).
- `Constants/WindowConstants.cs`: Window-related constants (e.g., default transparency, dimensions).
- `Helpers/TransparencyHelper.cs`: Transparency calculation helper, centralized management of transparency percentage conversion.
- `Helpers/DebounceHelper.cs`: Debounce helper for optimizing frequently triggered operations (e.g., window resizing).

**Infrastructure Layer (DesktopMemo.Infrastructure)**
- `Repositories/FileMemoRepository.cs`: File-based memo storage implementation (v2.2.0 and earlier).
- `Repositories/SqliteIndexedMemoRepository.cs`: SQLite-based memo storage implementation (v2.3.0+, with full-text search index).
- `Repositories/JsonTodoRepository.cs`: JSON file-based todo storage implementation.
- `Repositories/SqliteTodoRepository.cs`: SQLite-based todo storage implementation (v2.3.0+).
- `Services/JsonSettingsService.cs`: JSON-based application settings service.
- `Services/MemoSearchService.cs`: Memo search service, supports keyword search.
- `Services/WindowService.cs`: Window service, wraps Win32 API calls (topmost, transparency, etc.).
- `Services/TrayService.cs`: System tray service, manages tray icon and menu.
- `Services/MemoMigrationService.cs`: Memo data migration service (for version upgrades).
- `Services/TodoMigrationService.cs`: Todo data migration service (for version upgrades).

## 4. Build and Output

The project uses `Directory.Build.props` for unified output paths:

- Binary artifacts: `artifacts/v<version>/bin/<project-name>/<configuration>/<target-framework>/`
- Intermediate files: `artifacts/v<version>/obj/<project-name>/<configuration>/<target-framework>/`

`<version>` defaults to the `Version` property in `DesktopMemo.App.csproj` (currently `2.2.1`). To customize, set the `ArtifactsVersion` environment variable before building (example: in PowerShell, run `$env:ArtifactsVersion = '2.1.0'`).

Common commands:

```powershell
# Restore dependencies (must run first time or after changing output directory)
dotnet restore DesktopMemo.sln

# Debug build
dotnet build DesktopMemo.sln --configuration Debug

# Run application (reuses existing build artifacts)
dotnet run --project src/DesktopMemo.App/DesktopMemo.App.csproj --configuration Debug
```

> Recommendation: If you don't change BaseOutputPath/BaseIntermediateOutputPath or the version before building, clean the old directory (`artifacts/vX.X.X` for the corresponding version) first, then run restore + build to avoid issues caused by stale files.

## 5. Version Management

When manually updating versions, ensure the following locations are synchronized:

- `<Version>`, `<AssemblyVersion>`, `<FileVersion>` in `DesktopMemo.App/DesktopMemo.App.csproj`.
- Default value of `ArtifactsVersion` in `Directory.Build.props` (determines the `artifacts/vX.Y.Z/...` path).
- Fallback string in `MainViewModel.InitializeAppInfo()` (for UI display).
- Update `README.md` and other documentation.

After completing changes, it's recommended to run:

```powershell
dotnet clean DesktopMemo.sln
dotnet restore DesktopMemo.sln
dotnet build DesktopMemo.sln -c Debug
```

Ensure build artifacts are written to the new version directory and the application UI displays the correct version information.

## 6. Debugging Tips

- **Visual Studio**
  - Set `DesktopMemo.App` as the startup project, use WPF diagnostic tools to observe binding and layout.
  - Set breakpoints in `MainViewModel` to debug command execution and state transitions.
- **VS Code / Rider**
  - Use `dotnet run` or IDE-integrated debug configuration, ensure `--no-build` is only used when a previous build already exists.
- **Data Issue Troubleshooting**
  - Memo data: Check Markdown files in `/.memodata/content` and `index.json`
  - Todo data: Check `/.memodata/todos.json` file
  - Application settings: Check `/.memodata/settings.json` file
  - To reset data, delete the `.memodata` directory; the application will recreate it on next startup.

## 7. Release and Deployment (Draft)

1. Update `Version`/`FileVersion` in `DesktopMemo.App.csproj`.
2. Run `.\build_exe.bat`
3. Package the publish directory for release; for installer packages, configure with tools like MSIX/WiX separately.

## 8. Asynchronous Programming Best Practices

⚠️ **IMPORTANT**: This section contains critical guidelines to prevent application crashes. Please follow them.

### 8.1 async void Usage Guidelines

**❌ Wrong Approach - May Cause Deadlock Crashes**:
```csharp
private async void Button_Click(object sender, RoutedEventArgs e)
{
    await SomeAsyncOperation(); // Dangerous! May cause deadlock
}
```

**✅ Correct Approach**:
```csharp
private void Button_Click(object sender, RoutedEventArgs e)
{
    // Method 1: Use fire-and-forget pattern
    _ = Task.Run(async () => await HandleButtonClickAsync());
}

private async Task HandleButtonClickAsync()
{
    // Async logic goes here
}
```

### 8.2 UI Thread and Background Thread Switching

**Principle**: UI operations must be on UI thread, IO operations should be on background thread.

```csharp
// ✅ Correct thread switching pattern
await Task.Run(async () =>
{
    // Background thread executes IO operations
    await _settingsService.SaveAsync(settings);

    // Switch back to UI thread to update interface
    await Application.Current.Dispatcher.InvokeAsync(() =>
    {
        ViewModel.Settings = settings;
    });
});
```

### 8.3 Exception Handling Guidelines

```csharp
// ✅ Complete exception handling
try
{
    await SomeAsyncOperation();
}
catch (InvalidOperationException ex)
{
    // Handle specific exception
    Debug.WriteLine($"Invalid operation: {ex.Message}");
}
catch (UnauthorizedAccessException ex)
{
    // Handle permission issues
    ShowUserFriendlyError("Insufficient permissions");
}
catch (Exception ex)
{
    // Generic exception handling
    Debug.WriteLine($"Unknown error: {ex}");
    // Provide fallback solution
}
```

### 8.4 Settings Save Best Practices

```csharp
// ✅ Non-blocking settings save
if (dialog.DontShowAgain)
{
    // Don't wait for settings save to avoid blocking main operations
    _ = Task.Run(async () =>
    {
        try
        {
            var newSettings = settings with { ShowConfirmation = false };
            await _settingsService.SaveAsync(newSettings);

            // UI updates must be on UI thread
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Settings = newSettings;
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to save settings: {ex}");
            // Settings save failure should not affect main functionality
        }
    });
}
```

### 8.5 Debugging Async Issues

- **Use Debug Output**: Add `Debug.WriteLine` to key async operations
- **Check Thread ID**: Confirm operations execute on correct thread
- **Exception Logging**: Log complete exception stack, including inner exceptions

```csharp
Debug.WriteLine($"[Thread:{Thread.CurrentThread.ManagedThreadId}] Starting to save settings");
```

## 9. Code Architecture Guidelines

### 9.1 Opacity and Constant Management

**Use unified constant classes**:
```csharp
// ✅ Correct approach
using DesktopMemo.Core.Constants;
using DesktopMemo.Core.Helpers;

var opacity = WindowConstants.DEFAULT_TRANSPARENCY;
var percent = TransparencyHelper.ToPercent(opacity);
```

**❌ Avoid Hard-Coding**:
```csharp
// Wrong - hard-coded values
var opacity = 0.05;
var maxOpacity = 0.4;
```

### 9.2 Exception Handling Levels

Provide different levels of handling based on exception types:

```csharp
try
{
    // Operation code
}
catch (InvalidOperationException ex)
{
    // Level 1: Operation state exception - user-friendly message
    SetStatus($"Invalid operation: {ex.Message}");
}
catch (UnauthorizedAccessException ex)
{
    // Level 2: Permission exception - clear permission issue
    SetStatus($"Insufficient permissions: {ex.Message}");
}
catch (IOException ex)
{
    // Level 3: IO exception - filesystem problem
    SetStatus($"File operation failed: {ex.Message}");
}
catch (Exception ex)
{
    // Level 4: Unknown exception - log detailed information
    Debug.WriteLine($"Unknown exception: {ex}");
    SetStatus("Operation failed, please try again later");
}
```

### 9.3 Settings Save Atomicity

Ensure settings saves are atomic to avoid data inconsistency:

```csharp
// ✅ Atomic save
try
{
    var newSettings = currentSettings with { Property = newValue };
    await _settingsService.SaveAsync(newSettings); // Save to file first
    CurrentSettings = newSettings; // Update memory only after successful save
}
catch (Exception ex)
{
    // Save failed, memory state remains unchanged
    Debug.WriteLine($"Failed to save settings: {ex}");
}
```

### 9.4 Win32 API Call Guidelines

All Win32 API calls should include error checking:

```csharp
// ✅ Safe Win32 API call
private static void SafeSetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags, string operation)
{
    try
    {
        var success = SetWindowPos(hWnd, hWndInsertAfter, x, y, cx, cy, uFlags);
        if (!success)
        {
            var error = Marshal.GetLastWin32Error();
            Debug.WriteLine($"SetWindowPos failed - Operation: {operation}, Error code: {error}");
        }
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"SetWindowPos exception - Operation: {operation}, Exception: {ex.Message}");
    }
}
```

## 10. Quality Assurance

- The repository currently does not have automated testing; planned references can be found in `docs/architecture/migration-plan.md`.
- It's recommended to at least run `dotnet build` verification before PRs.
- If adding dependencies or adjusting output directories, please explain in the PR description.

## 11. Common Issues and Solutions

| Issue Type | Symptoms | Solution |
| --- | --- | --- |
| **Build Issues** | `NETSDK1005`: Cannot find `project.assets.json` | Run `dotnet restore` first, confirm new output directory is generated; if necessary, delete `artifacts/` and rebuild. |
| **Build Issues** | Duplicate assembly attributes | Delete old `bin/obj` and `artifacts/`, then re-run `restore + build`. |
| **UI Issues** | Data binding not refreshing | Check if `OnPropertyChanged` is called, confirm `MainViewModel` command execution path. |
| **Transparency Issues** | Transparency becomes 0% or 100% after restart | Check settings file integrity, verify correct usage of `TransparencyHelper`. |
| **Crash Issues** | Program freezes/crashes after checking "Don't show again" | Check if `async void` event handlers are used, switch to `Task.Run` + fire-and-forget pattern. |
| **Dialog Issues** | Confirmation dialog cannot be displayed | Check if `Owner` is set correctly, ensure dialog is created on UI thread. |
| **Settings Issues** | Settings save failure causes data inconsistency | Use atomic save pattern: save file first, then update memory state on success. |
| **Win32 Issues** | Window topmost or transparency setting fails | Check Win32 API return value, use `Marshal.GetLastWin32Error()` to get error code. |

### Debugging Tips

**Enable Detailed Debug Logging**:
In Debug mode, the application outputs detailed debug information to Visual Studio's output window:

```csharp
// View settings loading process
Debug.WriteLine($"Settings loaded: original transparency={settings.Transparency}, normalized={normalizedValue}");

// View async operation status
Debug.WriteLine($"[Thread:{Thread.CurrentThread.ManagedThreadId}] Starting to save settings");

// View Win32 API call results
Debug.WriteLine($"SetWindowPos failed - Operation: {operation}, Error code: {error}");
```

**Check Settings File**:
Settings file location: `Application directory/.memodata/settings.json`

If settings are abnormal, you can:
1. Delete `settings.json` file to reset to default settings
2. Check if file content is valid JSON format
3. View backup file `settings.json.backup` (if exists)

**Performance Analysis**:
- Use Visual Studio's diagnostic tools to monitor memory and CPU usage
- Check if async operations block UI thread
- Check for memory leaks (especially event subscriptions)

For more issues, please provide feedback in the repository [Issues](../../../issues) section.

## 12. Future: MySQL Cloud Storage Integration

DesktopMemo is planning to add MySQL-based cloud storage and sync capabilities. This feature will allow users to sync their memos and todos across multiple devices.

### Current Status
- ✅ Local SQLite storage implemented (v2.3.0)
- ✅ Sync status fields added to data models
- 📝 MySQL integration specification defined

### Key Features (Planned)
- Optional cloud sync (user opt-in)
- Multi-device synchronization
- Conflict resolution strategies
- End-to-end encryption support
- Incremental sync for performance

### Architecture
The MySQL integration will follow a "local-first" architecture:
- Local SQLite remains the primary data source
- Cloud sync operates asynchronously in the background
- Changes are tracked using version numbers and timestamps
- Conflicts are resolved using configurable strategies

### Implementation Guidelines
For detailed technical specifications, API design, data models, and implementation guidelines, please refer to:
- English: [MySQL Integration Specification](MySQL-Integration-Specification.md)
- 中文: [MySQL 集成规范](MySQL-集成规范.md)

### Migration Path
1. **Phase 1: Preparation** (Current - v2.3.0)
   - Local SQLite storage
   - Sync-ready data models
2. **Phase 2: Backend Development**
   - MySQL schema design
   - REST API implementation
   - Authentication system
3. **Phase 3: Client Integration**
   - Cloud sync service
   - UI components
   - Conflict resolution
4. **Phase 4: Testing & Rollout**
   - Beta testing
   - Gradual rollout
   - Monitoring and iteration

### Contributing
If you're interested in contributing to the MySQL integration:
1. Review the [MySQL Integration Specification](MySQL-Integration-Specification.md)
2. Check existing issues tagged with `cloud-sync`
3. Discuss your approach in the issue before implementing
4. Follow the coding standards and async programming best practices outlined in this document

