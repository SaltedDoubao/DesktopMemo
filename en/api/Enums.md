# Enum Types

This document describes all enum types used in DesktopMemo.

---

## TopmostMode

Window topmost mode.

### Definition

```csharp
namespace DesktopMemo.Core.Contracts;

/// <summary>
/// Window topmost mode.
/// </summary>
public enum TopmostMode
{
    /// <summary>
    /// Normal mode (not topmost).
    /// </summary>
    Normal = 0,

    /// <summary>
    /// Desktop-layer topmost (above desktop icons, below other app windows).
    /// </summary>
    Desktop = 1,

    /// <summary>
    /// Always on top (above all windows).
    /// </summary>
    Always = 2
}
```

### Values

| Value | Name | Description | Typical usage |
|---:|---|---|---|
| 0 | **Normal** | Normal window (not topmost) | Can be covered by other windows |
| 1 | **Desktop** | Desktop-layer topmost | Above desktop icons, below other app windows (default) |
| 2 | **Always** | Always on top | Stays above all windows |

---

### Examples

#### Set topmost mode

```csharp
_windowService.SetTopmostMode(TopmostMode.Normal);
_windowService.SetTopmostMode(TopmostMode.Desktop);
_windowService.SetTopmostMode(TopmostMode.Always);
```

---

#### Toggle from tray menu

```csharp
_trayService.TopmostModeChangeClick += (sender, mode) =>
{
    _windowService.SetTopmostMode(mode);
    _trayService.UpdateTopmostState(mode);
};
```

---

#### Restore from settings

```csharp
public void ApplySettings(WindowSettings settings)
{
    TopmostMode mode;

    if (settings.IsTopMost)
    {
        mode = TopmostMode.Always;
    }
    else if (settings.IsDesktopMode)
    {
        mode = TopmostMode.Desktop;
    }
    else
    {
        mode = TopmostMode.Normal;
    }

    _windowService.SetTopmostMode(mode);
}
```

---

### Implementation Notes

#### Normal mode
- WPF: `window.Topmost = false`
- Win32: no special API calls

#### Desktop mode
- Win32 API: `SetWindowPos(hwnd, HWND_BOTTOM, ...)`
- Places the window on the desktop layer in the Z-order

#### Always mode
- WPF: `window.Topmost = true`
- Keeps the window at the top of the Z-order

---

### Notes

- **Desktop** is the recommended default: visible without blocking other apps
- **Always** is useful for critical always-visible information
- **Normal** is suitable when you want to send the window behind other apps
- After switching modes, tray menu state should be updated accordingly

---

## AppTheme

Application theme.

### Definition

```csharp
namespace DesktopMemo.Core.Models;

/// <summary>
/// Application theme type.
/// </summary>
public enum AppTheme
{
    /// <summary>
    /// Light theme.
    /// </summary>
    Light = 0,

    /// <summary>
    /// Dark theme.
    /// </summary>
    Dark = 1,

    /// <summary>
    /// Follow the system theme.
    /// </summary>
    System = 2
}
```

### Values

| Value | Name | Description |
|---:|---|---|
| 0 | **Light** | Light theme (white background) |
| 1 | **Dark** | Dark theme (dark background) |
| 2 | **System** | Follow Windows 10/11 system theme |

---

### Examples

#### Switch theme

```csharp
var newSettings = settings with { Theme = AppTheme.Dark };
await _settingsService.SaveAsync(newSettings);
ApplyTheme(AppTheme.Dark);

var newSettings = settings with { Theme = AppTheme.Light };
await _settingsService.SaveAsync(newSettings);
ApplyTheme(AppTheme.Light);

var newSettings = settings with { Theme = AppTheme.System };
await _settingsService.SaveAsync(newSettings);
ApplyTheme(AppTheme.System);
```

---

#### Apply theme resources

```csharp
private void ApplyTheme(AppTheme theme)
{
    var lightTheme = new Uri("pack://application:,,,/Resources/Themes/Light.xaml");
    var darkTheme = new Uri("pack://application:,,,/Resources/Themes/Dark.xaml");

    var resources = Application.Current.Resources.MergedDictionaries;

    // Remove existing theme
    var existingTheme = resources.FirstOrDefault(d =>
        d.Source == lightTheme || d.Source == darkTheme);
    if (existingTheme != null)
    {
        resources.Remove(existingTheme);
    }

    // Apply new theme
    Uri themeUri;
    if (theme == AppTheme.System)
    {
        var systemTheme = GetSystemTheme();
        themeUri = systemTheme == "Dark" ? darkTheme : lightTheme;
    }
    else
    {
        themeUri = theme == AppTheme.Dark ? darkTheme : lightTheme;
    }

    resources.Add(new ResourceDictionary { Source = themeUri });
}
```

---

#### Detect system theme

```csharp
private string GetSystemTheme()
{
    try
    {
        using var key = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        var value = key?.GetValue("AppsUseLightTheme");

        return value is int intValue && intValue == 0 ? "Dark" : "Light";
    }
    catch
    {
        return "Light"; // default
    }
}
```

---

### Theme Resource Files

#### `Light.xaml`

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- Light theme colors -->
    <SolidColorBrush x:Key="PrimaryBackgroundBrush" Color="#FFFFFF"/>
    <SolidColorBrush x:Key="PrimaryForegroundBrush" Color="#000000"/>
    <SolidColorBrush x:Key="SecondaryBackgroundBrush" Color="#F5F5F5"/>
    <!-- more... -->

</ResourceDictionary>
```

#### `Dark.xaml`

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- Dark theme colors -->
    <SolidColorBrush x:Key="PrimaryBackgroundBrush" Color="#1E1E1E"/>
    <SolidColorBrush x:Key="PrimaryForegroundBrush" Color="#FFFFFF"/>
    <SolidColorBrush x:Key="SecondaryBackgroundBrush" Color="#2D2D2D"/>
    <!-- more... -->

</ResourceDictionary>
```

---

### Notes

- After switching themes, the UI should be refreshed
- Prefer dynamic resource references so the UI updates immediately
- System mode should listen to Windows theme change events

---

## SyncStatus

Data sync status (planned for future cloud sync).

### Definition

```csharp
namespace DesktopMemo.Core.Models;

/// <summary>
/// Data sync status.
/// </summary>
public enum SyncStatus
{
    /// <summary>
    /// Synced to the cloud.
    /// </summary>
    Synced = 0,

    /// <summary>
    /// Modified locally; pending upload.
    /// </summary>
    PendingSync = 1,

    /// <summary>
    /// Conflict detected.
    /// </summary>
    Conflict = 2
}
```

### Values

| Value | Name | Description | UI |
|---:|---|---|---|
| 0 | **Synced** | Local data matches cloud | ✓ green check |
| 1 | **PendingSync** | Local changes pending upload | ⏱ pending |
| 2 | **Conflict** | Both local and cloud changed | ⚠ warning |

---

### Examples

#### Display status text

```csharp
public string GetSyncStatusText(SyncStatus status)
{
    return status switch
    {
        SyncStatus.Synced => "Synced",
        SyncStatus.PendingSync => "Pending",
        SyncStatus.Conflict => "Conflict",
        _ => "Unknown"
    };
}
```

---

#### Show icon by status

```xaml
<DataTemplate x:Key="SyncStatusTemplate">
    <StackPanel Orientation="Horizontal">
        <TextBlock>
            <TextBlock.Style>
                <Style TargetType="TextBlock">
                    <Setter Property="Text" Value="✓"/>
                    <Setter Property="Foreground" Value="Green"/>
                    <Style.Triggers>
                        <DataTrigger Binding="{Binding SyncStatus}" Value="PendingSync">
                            <Setter Property="Text" Value="⏱"/>
                            <Setter Property="Foreground" Value="Orange"/>
                        </DataTrigger>
                        <DataTrigger Binding="{Binding SyncStatus}" Value="Conflict">
                            <Setter Property="Text" Value="⚠"/>
                            <Setter Property="Foreground" Value="Red"/>
                        </DataTrigger>
                    </Style.Triggers>
                </Style>
            </TextBlock.Style>
        </TextBlock>
    </StackPanel>
</DataTemplate>
```

---

#### Example sync flow

```csharp
public async Task SyncMemoAsync(Memo memo)
{
    if (memo.SyncStatus == SyncStatus.Synced)
    {
        return;
    }

    if (memo.SyncStatus == SyncStatus.Conflict)
    {
        await ResolveConflictAsync(memo);
        return;
    }

    try
    {
        var remoteVersion = await _cloudService.UploadMemoAsync(memo);
        var synced = memo.MarkAsSynced(remoteVersion);
        await _repository.UpdateAsync(synced);
    }
    catch (ConflictException)
    {
        var conflicted = memo.MarkAsConflict();
        await _repository.UpdateAsync(conflicted);
    }
}
```

---

### State transitions

```
CreateNew
   ↓
Synced (initial)
   ↓ (local changes)
PendingSync
   ↓ (upload success)
Synced
   ↓ (conflict detected)
Conflict
   ↓ (resolved)
Synced
```

---

### Notes

- Cloud sync is not implemented in v2.3.0, but the data model is prepared
- Newly created data defaults to `Synced`
- Any local change is automatically marked as `PendingSync`
- Conflicts require user resolution

---

## LogLevel

Log level.

### Definition

```csharp
namespace DesktopMemo.Core.Models;

/// <summary>
/// Log level.
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Debug information.
    /// </summary>
    Debug = 0,

    /// <summary>
    /// General info.
    /// </summary>
    Info = 1,

    /// <summary>
    /// Warning.
    /// </summary>
    Warning = 2,

    /// <summary>
    /// Error.
    /// </summary>
    Error = 3,

    /// <summary>
    /// Fatal error.
    /// </summary>
    Fatal = 4
}
```

### Values

| Value | Name | Description | Typical usage |
|---:|---|---|---|
| 0 | **Debug** | Debug information | Detailed traces during development |
| 1 | **Info** | General information | Normal operation records |
| 2 | **Warning** | Warning | Potential issue; functionality still works |
| 3 | **Error** | Error | Operation failed; needs attention |
| 4 | **Fatal** | Fatal | Severe issue; may crash the app |

---

### Examples

#### Log messages at different levels

```csharp
_logService.Log("Start loading memos", LogLevel.Debug);
_logService.Log("App started successfully", LogLevel.Info);
_logService.Log("Settings file corrupted; restored from backup", LogLevel.Warning);
_logService.Log("Failed to save memo: access denied", LogLevel.Error);
_logService.Log("Database connection failed; cannot continue", LogLevel.Fatal);
```

---

#### Filter logs by level

```csharp
public async Task<IEnumerable<LogEntry>> GetErrorLogsAsync()
{
    var allLogs = await _logService.GetLogsAsync();
    return allLogs.Where(log => log.Level >= LogLevel.Error);
}
```

---

#### Choose colors by level

```csharp
public Brush GetLogLevelBrush(LogLevel level)
{
    return level switch
    {
        LogLevel.Debug => Brushes.Gray,
        LogLevel.Info => Brushes.Black,
        LogLevel.Warning => Brushes.Orange,
        LogLevel.Error => Brushes.Red,
        LogLevel.Fatal => Brushes.DarkRed,
        _ => Brushes.Black
    };
}
```

---

### Guidelines for each level

#### Debug

```csharp
_logService.Log($"Enter method: LoadMemosAsync, count={count}", LogLevel.Debug);
_logService.Log($"SQL: {query}", LogLevel.Debug);
```

#### Info

```csharp
_logService.Log("Memo created successfully", LogLevel.Info);
_logService.Log("Settings saved successfully", LogLevel.Info);
```

#### Warning

```csharp
_logService.Log("Cannot load user settings; using defaults", LogLevel.Warning);
_logService.Log("Partial migration failure; skipped", LogLevel.Warning);
```

#### Error

```csharp
_logService.Log($"Failed to save memo: {ex.Message}", LogLevel.Error);
_logService.Log("File system access denied", LogLevel.Error);
```

#### Fatal

```csharp
_logService.Log("DB initialization failed; app cannot start", LogLevel.Fatal);
_logService.Log($"Unhandled exception: {ex}", LogLevel.Fatal);
```

---

## Enum Conversion Helpers

### Convert between TopmostMode and settings

```csharp
public static class TopmostModeExtensions
{
    public static (bool IsTopMost, bool IsDesktopMode) ToSettings(this TopmostMode mode)
    {
        return mode switch
        {
            TopmostMode.Normal => (false, false),
            TopmostMode.Desktop => (false, true),
            TopmostMode.Always => (true, false),
            _ => (false, true)
        };
    }

    public static TopmostMode FromSettings(bool isTopMost, bool isDesktopMode)
    {
        if (isTopMost) return TopmostMode.Always;
        if (isDesktopMode) return TopmostMode.Desktop;
        return TopmostMode.Normal;
    }
}
```

Usage:
```csharp
var mode = TopmostModeExtensions.FromSettings(
    settings.IsTopMost,
    settings.IsDesktopMode
);

var (isTopMost, isDesktopMode) = mode.ToSettings();
var newSettings = settings with
{
    IsTopMost = isTopMost,
    IsDesktopMode = isDesktopMode
};
```

---

### Localize AppTheme

```csharp
public static class AppThemeExtensions
{
    public static string GetDisplayName(this AppTheme theme, ILocalizationService localization)
    {
        return theme switch
        {
            AppTheme.Light => localization["Theme_Light"],
            AppTheme.Dark => localization["Theme_Dark"],
            AppTheme.System => localization["Theme_System"],
            _ => theme.ToString()
        };
    }
}
```

Resource file example (`Strings.resx`):
```xml
<data name="Theme_Light" xml:space="preserve">
    <value>Light</value>
</data>
<data name="Theme_Dark" xml:space="preserve">
    <value>Dark</value>
</data>
<data name="Theme_System" xml:space="preserve">
    <value>System</value>
</data>
```

---

## Best Practices

### 1. Use explicit enum values

```csharp
_windowService.SetTopmostMode(TopmostMode.Desktop); // ✅
_windowService.SetTopmostMode((TopmostMode)1);      // ❌ magic number
```

---

### 2. Handle unknown values

```csharp
var text = status switch
{
    SyncStatus.Synced => "Synced",
    SyncStatus.PendingSync => "Pending",
    SyncStatus.Conflict => "Conflict",
    _ => "Unknown"
};
```

---

### 3. Enum serialization

```csharp
var options = new JsonSerializerOptions
{
    Converters = { new JsonStringEnumConverter() }
};

var json = JsonSerializer.Serialize(settings, options);
```

---

## Related Docs

- [Domain Models](./Models.md)
- [Service Interfaces](./Services.md)
- [Repository Interfaces](./Repositories.md)

---

**Last Updated**: 2025-11-15
