# Service Interfaces

Service interfaces provide business logic and system functionality, including settings management, search, window management, tray service, and localization.

---

## ISettingsService

Application settings service for loading and saving settings.

### Interface Definition

```csharp
namespace DesktopMemo.Core.Contracts;

/// <summary>
/// Provides interfaces for loading and saving application settings.
/// </summary>
public interface ISettingsService
{
    Task<WindowSettings> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(WindowSettings settings, CancellationToken cancellationToken = default);
}
```

### Method Details

#### LoadAsync

Loads application settings.

Signature:
```csharp
Task<WindowSettings> LoadAsync(CancellationToken cancellationToken = default)
```

Returns:
- `Task<WindowSettings>` - settings loaded from disk; returns defaults if the file does not exist

Example:
```csharp
var settings = await _settingsService.LoadAsync();
Console.WriteLine($"Window size: {settings.Width}x{settings.Height}");
Console.WriteLine($"Transparency: {settings.Transparency}");
```

Notes:
- If the settings file does not exist, returns `WindowSettings.Default`
- If the file is corrupted, tries to restore from backup
- On failure, logs the error and returns default settings

---

#### SaveAsync

Saves application settings.

Signature:
```csharp
Task SaveAsync(WindowSettings settings, CancellationToken cancellationToken = default)
```

Parameters:
- `settings` - settings to save

Example:
```csharp
var newSettings = currentSettings.WithSize(1200, 800);
await _settingsService.SaveAsync(newSettings);
```

Notes:
- Uses atomic save: write to a temp file, then replace the original
- Creates a backup file before overwriting
- Stored in JSON format

Paths:
- Main file: `.memodata/settings.json`
- Backup file: `.memodata/settings.json.backup`

Best practice (atomic update):
```csharp
try
{
    var newSettings = currentSettings with { Transparency = 0.2 };
    await _settingsService.SaveAsync(newSettings); // persist first
    CurrentSettings = newSettings; // update in-memory state only on success
}
catch (Exception ex)
{
    Debug.WriteLine($"Failed to save settings: {ex}");
}
```

---

## IMemoSearchService

Memo search service, providing find and replace.

### Interface Definition

```csharp
namespace DesktopMemo.Core.Contracts;

public interface IMemoSearchService
{
    IEnumerable<SearchMatch> FindMatches(string text, string keyword,
        bool caseSensitive = false, bool useRegex = false);

    string Replace(string text, string keyword, string replacement,
        bool caseSensitive = false, bool useRegex = false);
}

public sealed record SearchMatch(int Index, int Length);
```

### Method Details

#### FindMatches

Finds all matches in a text.

Signature:
```csharp
IEnumerable<SearchMatch> FindMatches(string text, string keyword,
    bool caseSensitive = false, bool useRegex = false)
```

Parameters:
- `text` - input text
- `keyword` - search keyword or regex pattern
- `caseSensitive` - case-sensitive match (default: false)
- `useRegex` - treat keyword as regex (default: false)

Returns:
- `IEnumerable<SearchMatch>` - match list containing index and length

Example:
```csharp
var text = "Hello World, hello universe";
var matches = _searchService.FindMatches(text, "hello", caseSensitive: false);

foreach (var match in matches)
{
    Console.WriteLine($"Match: index={match.Index}, length={match.Length}");
    var matchedText = text.Substring(match.Index, match.Length);
    Console.WriteLine($"Text: {matchedText}");
}
```

Regex example:
```csharp
var matches = _searchService.FindMatches(
    text,
    @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b",
    useRegex: true
);
```

---

#### Replace

Replaces all matches.

Signature:
```csharp
string Replace(string text, string keyword, string replacement,
    bool caseSensitive = false, bool useRegex = false)
```

Parameters:
- `text` - original text
- `keyword` - keyword or regex pattern
- `replacement` - replacement text
- `caseSensitive` - case-sensitive match
- `useRegex` - regex mode

Returns:
- `string` - replaced text

Example:
```csharp
var text = "Hello World, hello universe";
var result = _searchService.Replace(text, "hello", "Hi", caseSensitive: false);
Console.WriteLine(result);
```

Regex replacement example:
```csharp
var result = _searchService.Replace(
    "Version 2.3.0 released on 2025-11-15",
    @"\d+",
    "[NUM]",
    useRegex: true
);
```

---

## IWindowService

Window management service: topmost mode, transparency, click-through, positioning, and tray integration.

### Interface Definition

```csharp
namespace DesktopMemo.Core.Contracts;

public enum TopmostMode
{
    Normal,     // normal mode
    Desktop,    // desktop-layer topmost
    Always      // always on top
}

public interface IWindowService
{
    void SetTopmostMode(TopmostMode mode);
    TopmostMode GetCurrentTopmostMode();
    void SetClickThrough(bool enabled);
    bool IsClickThroughEnabled { get; }
    void SetWindowPosition(double x, double y);
    (double X, double Y) GetWindowPosition();
    void MoveToPresetPosition(string position);
    void SetWindowOpacity(double opacity);
    double GetWindowOpacity();
    void PlayFadeInAnimation();
    void ToggleWindowVisibility();
    void MinimizeToTray();
    void RestoreFromTray();
}
```

### Method Details

#### SetTopmostMode

Sets window topmost mode.

Signature:
```csharp
void SetTopmostMode(TopmostMode mode)
```

Parameters:
- `mode` - `Normal` / `Desktop` / `Always`

Example:
```csharp
_windowService.SetTopmostMode(TopmostMode.Always);
_windowService.SetTopmostMode(TopmostMode.Normal);
```

Notes:
- `Desktop` uses Win32 API to place the window above desktop icons
- `Always` uses WPF `Topmost`
- Tray menu state should be updated when switching modes

---

#### SetClickThrough

Enables/disables click-through.

Signature:
```csharp
void SetClickThrough(bool enabled)
```

Example:
```csharp
_windowService.SetClickThrough(true);
_windowService.SetClickThrough(false);
```

Notes:
- Clicks pass through the window to the underlying applications
- Implemented by setting Win32 extended style `WS_EX_TRANSPARENT`
- Tray menu can still control the window while click-through is enabled

---

#### SetWindowPosition

Sets window position (screen coordinates).

Signature:
```csharp
void SetWindowPosition(double x, double y)
```

Example:
```csharp
_windowService.SetWindowPosition(0, 0);

var screenWidth = SystemParameters.PrimaryScreenWidth;
var screenHeight = SystemParameters.PrimaryScreenHeight;
_windowService.SetWindowPosition(
    (screenWidth - windowWidth) / 2,
    (screenHeight - windowHeight) / 2
);
```

---

#### MoveToPresetPosition

Moves to a preset position.

Signature:
```csharp
void MoveToPresetPosition(string position)
```

Allowed values:
- `"top-left"`, `"top-right"`, `"bottom-left"`, `"bottom-right"`, `"center"`

Example:
```csharp
_windowService.MoveToPresetPosition("bottom-right");
_windowService.MoveToPresetPosition("center");
```

---

#### SetWindowOpacity

Sets window opacity.

Signature:
```csharp
void SetWindowOpacity(double opacity)
```

Parameters:
- `opacity` - `0.0` to `1.0` (app limits to `0.05`–`0.4`)

Example:
```csharp
_windowService.SetWindowOpacity(0.2);

using DesktopMemo.Core.Constants;
_windowService.SetWindowOpacity(WindowConstants.DEFAULT_TRANSPARENCY);
```

Notes:
- Prefer `WindowConstants` and `TransparencyHelper` for consistent opacity management

---

#### MinimizeToTray / RestoreFromTray

Example:
```csharp
_windowService.MinimizeToTray();
_windowService.RestoreFromTray();
```

---

## ITrayService

System tray service for tray icon and menus.

### Interface Definition

```csharp
namespace DesktopMemo.Core.Contracts;

public interface ITrayService : IDisposable
{
    // Events
    event EventHandler? TrayIconDoubleClick;
    event EventHandler? ShowHideWindowClick;
    event EventHandler? NewMemoClick;
    event EventHandler? SettingsClick;
    event EventHandler? ExitClick;
    event EventHandler<string>? MoveToPresetClick;
    event EventHandler? RememberPositionClick;
    event EventHandler? RestorePositionClick;
    event EventHandler<bool>? ClickThroughToggleClick;
    event EventHandler<TopmostMode>? TopmostModeChangeClick;

    // Methods
    void Initialize();
    void Show();
    void Hide();
    void ShowBalloonTip(string title, string text, int timeout = 2000);
    void UpdateText(string text);
    void UpdateTopmostState(TopmostMode mode);
    void UpdateClickThroughState(bool enabled);
    void UpdateMenuTexts(Func<string, string> getLocalizedString);
}
```

### Method Details

#### Initialize

Initializes the tray icon and menu.

Example:
```csharp
_trayService.Initialize();
_trayService.Show();
```

---

#### ShowBalloonTip

Shows a balloon tip.

Signature:
```csharp
void ShowBalloonTip(string title, string text, int timeout = 2000)
```

Parameters:
- `title` - title
- `text` - message
- `timeout` - milliseconds (default: 2000)

Example:
```csharp
_trayService.ShowBalloonTip("Saved", "Memo has been saved", 3000);
```

---

#### UpdateMenuTexts

Updates tray menu texts (for language switching).

Example:
```csharp
_trayService.UpdateMenuTexts(key => _localizationService[key]);
```

---

## ILocalizationService

Localization service.

### Interface Definition

```csharp
namespace DesktopMemo.Core.Contracts;

public interface ILocalizationService
{
    string this[string key] { get; }
    CultureInfo CurrentCulture { get; }
    void ChangeLanguage(string cultureName);
    IEnumerable<CultureInfo> GetSupportedLanguages();
    event EventHandler<LanguageChangedEventArgs>? LanguageChanged;
}
```

### Method Details

#### Get localized strings

Example:
```csharp
var saveText = _localizationService["Save"];
var cancelText = _localizationService["Cancel"];

// In XAML:
// <TextBlock Text="{loc:Localize Save}" />
```

---

#### ChangeLanguage

Switches language.

Signature:
```csharp
void ChangeLanguage(string cultureName)
```

Common values:
- `"zh-CN"` - Simplified Chinese
- `"en-US"` - English
- `"zh-TW"` - Traditional Chinese
- `"ja-JP"` - Japanese
- `"ko-KR"` - Korean

Example:
```csharp
_localizationService.ChangeLanguage("en-US");

_localizationService.LanguageChanged += (s, e) =>
{
    Console.WriteLine($"Language switched: {e.OldCulture} -> {e.NewCulture}");
    _trayService.UpdateMenuTexts(key => _localizationService[key]);
};
```

---

#### GetSupportedLanguages

Returns supported languages.

Example:
```csharp
var languages = _localizationService.GetSupportedLanguages();
foreach (var lang in languages)
{
    Console.WriteLine($"{lang.DisplayName} ({lang.Name})");
}
```

---

## ILogService

Logging service.

### Interface Definition

```csharp
namespace DesktopMemo.Core.Contracts;

public interface ILogService
{
    void Log(string message, LogLevel level = LogLevel.Info);
    Task<IEnumerable<LogEntry>> GetLogsAsync(int count = 100);
    Task ClearLogsAsync();
}
```

### Method Details

#### Log

Example:
```csharp
_logService.Log("App started", LogLevel.Info);
_logService.Log("Save failed", LogLevel.Error);
```

---

## Implementations

### JsonSettingsService
- Location: `DesktopMemo.Infrastructure.Services`
- Uses JSON to store settings
- Supports backup and restore

### MemoSearchService
- Location: `DesktopMemo.Infrastructure.Services`
- Supports plain text and regex search
- Case-insensitive option

### WindowService
- Location: `DesktopMemo.Infrastructure.Services`
- Uses Win32 APIs to control the window
- Supports multiple topmost modes and transparency

### TrayService
- Location: `DesktopMemo.Infrastructure.Services`
- Uses WinForms NotifyIcon
- Provides a rich tray menu

### LocalizationService
- Location: `DesktopMemo.App.Localization`
- Uses resource files
- Supports dynamic language switching

### FileLogService
- Location: `DesktopMemo.Infrastructure.Services`
- Logs stored under `.memodata/.logs/`
- Rotates by date automatically

---

## Best Practices

### 1. Atomic settings saves

```csharp
try
{
    var newSettings = currentSettings with { Theme = AppTheme.Dark };
    await _settingsService.SaveAsync(newSettings);
    CurrentSettings = newSettings;
}
catch (Exception ex)
{
    Debug.WriteLine($"Save failed: {ex}");
}
```

---

### 2. Using the search service

```csharp
var matches = _searchService.FindMatches(content, keyword);
foreach (var match in matches)
{
    HighlightText(match.Index, match.Length);
}
```

---

### 3. Coordinating window and settings state

```csharp
_windowService.SetTopmostMode(TopmostMode.Always);
_trayService.UpdateTopmostState(TopmostMode.Always);

var newSettings = settings with { IsTopMost = true, IsDesktopMode = false };
await _settingsService.SaveAsync(newSettings);
```

---

## Related Docs

- [Repository Interfaces](./Repositories.md)
- [Domain Models](./Models.md)
- [Enum Types](./Enums.md)

---

**Last Updated**: 2025-11-15
