# 服务接口文档

服务接口提供业务逻辑和系统功能，包括设置管理、搜索、窗口管理、托盘服务和本地化等。

---

## ISettingsService

应用设置管理服务，提供设置的加载和保存功能。

### 接口定义

```csharp
namespace DesktopMemo.Core.Contracts;

/// <summary>
/// 提供应用设置的加载与保存接口。
/// </summary>
public interface ISettingsService
{
    Task<WindowSettings> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(WindowSettings settings, CancellationToken cancellationToken = default);
}
```

### 方法说明

#### LoadAsync

加载应用设置。

**签名**
```csharp
Task<WindowSettings> LoadAsync(CancellationToken cancellationToken = default)
```

**返回值**
- `Task<WindowSettings>` - 加载的设置对象，如果文件不存在则返回默认设置

**示例**
```csharp
var settings = await _settingsService.LoadAsync();
Console.WriteLine($"窗口大小: {settings.Width}x{settings.Height}");
Console.WriteLine($"透明度: {settings.Transparency}");
```

**注意事项**
- 如果设置文件不存在，返回 `WindowSettings.Default`
- 如果文件损坏，会尝试从备份恢复
- 加载失败时记录错误日志并返回默认设置

---

#### SaveAsync

保存应用设置。

**签名**
```csharp
Task SaveAsync(WindowSettings settings, CancellationToken cancellationToken = default)
```

**参数**
- `settings` - 要保存的设置对象

**示例**
```csharp
var newSettings = currentSettings.WithSize(1200, 800);
await _settingsService.SaveAsync(newSettings);
```

**注意事项**
- 采用原子性保存：先写入临时文件，成功后替换原文件
- 保存前会创建备份文件
- 使用 JSON 格式存储

**保存位置**
- 主文件：`.memodata/settings.json`
- 备份文件：`.memodata/settings.json.backup`

**最佳实践：原子性更新**
```csharp
try
{
    var newSettings = currentSettings with { Transparency = 0.2 };
    await _settingsService.SaveAsync(newSettings); // 先保存到文件
    CurrentSettings = newSettings; // 保存成功后才更新内存
}
catch (Exception ex)
{
    // 保存失败，内存状态保持不变
    Debug.WriteLine($"设置保存失败: {ex}");
}
```

---

## IMemoSearchService

备忘录搜索服务，提供文本搜索和替换功能。

### 接口定义

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

### 方法说明

#### FindMatches

在文本中查找所有匹配项。

**签名**
```csharp
IEnumerable<SearchMatch> FindMatches(string text, string keyword,
    bool caseSensitive = false, bool useRegex = false)
```

**参数**
- `text` - 要搜索的文本
- `keyword` - 搜索关键词或正则表达式
- `caseSensitive` - 是否区分大小写（默认：false）
- `useRegex` - 是否使用正则表达式（默认：false）

**返回值**
- `IEnumerable<SearchMatch>` - 匹配项集合，包含位置和长度

**示例**
```csharp
var text = "Hello World, hello universe";
var matches = _searchService.FindMatches(text, "hello", caseSensitive: false);

foreach (var match in matches)
{
    Console.WriteLine($"找到匹配: 位置={match.Index}, 长度={match.Length}");
    var matchedText = text.Substring(match.Index, match.Length);
    Console.WriteLine($"内容: {matchedText}");
}

// 输出:
// 找到匹配: 位置=0, 长度=5
// 内容: Hello
// 找到匹配: 位置=13, 长度=5
// 内容: hello
```

**正则表达式示例**
```csharp
// 查找所有邮箱地址
var matches = _searchService.FindMatches(
    text,
    @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b",
    useRegex: true
);
```

---

#### Replace

替换文本中的所有匹配项。

**签名**
```csharp
string Replace(string text, string keyword, string replacement,
    bool caseSensitive = false, bool useRegex = false)
```

**参数**
- `text` - 原始文本
- `keyword` - 要查找的关键词或正则表达式
- `replacement` - 替换文本
- `caseSensitive` - 是否区分大小写
- `useRegex` - 是否使用正则表达式

**返回值**
- `string` - 替换后的文本

**示例**
```csharp
var text = "Hello World, hello universe";
var result = _searchService.Replace(text, "hello", "Hi", caseSensitive: false);
Console.WriteLine(result);
// 输出: Hi World, Hi universe
```

**正则表达式替换示例**
```csharp
// 将所有数字替换为 [NUM]
var result = _searchService.Replace(
    "版本 2.3.0 发布于 2025-11-15",
    @"\d+",
    "[NUM]",
    useRegex: true
);
// 结果: 版本 [NUM].[NUM].[NUM] 发布于 [NUM]-[NUM]-[NUM]
```

---

## IWindowService

窗口管理服务，提供窗口置顶、透明度、穿透模式等功能。

### 接口定义

```csharp
namespace DesktopMemo.Core.Contracts;

public enum TopmostMode
{
    Normal,     // 普通模式，不置顶
    Desktop,    // 桌面层面置顶
    Always      // 总是置顶
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

### 方法说明

#### SetTopmostMode

设置窗口置顶模式。

**签名**
```csharp
void SetTopmostMode(TopmostMode mode)
```

**参数**
- `mode` - 置顶模式
  - `Normal` - 普通模式，不置顶
  - `Desktop` - 桌面层面置顶（低于其他应用）
  - `Always` - 总是置顶（高于所有窗口）

**示例**
```csharp
// 设置为总是置顶
_windowService.SetTopmostMode(TopmostMode.Always);

// 取消置顶
_windowService.SetTopmostMode(TopmostMode.Normal);
```

**注意事项**
- `Desktop` 模式使用 Win32 API 将窗口置于桌面图标之上
- `Always` 模式设置 WPF 的 `Topmost` 属性
- 切换模式时会自动更新托盘菜单状态

---

#### SetClickThrough

启用或禁用穿透模式。

**签名**
```csharp
void SetClickThrough(bool enabled)
```

**参数**
- `enabled` - 是否启用穿透模式

**示例**
```csharp
// 启用穿透模式
_windowService.SetClickThrough(true);

// 禁用穿透模式
_windowService.SetClickThrough(false);
```

**注意事项**
- 穿透模式下，鼠标点击会穿透窗口到下层应用
- 通过 Win32 API 设置窗口扩展样式 `WS_EX_TRANSPARENT`
- 穿透模式下仍可通过托盘菜单控制窗口

---

#### SetWindowPosition

设置窗口位置。

**签名**
```csharp
void SetWindowPosition(double x, double y)
```

**参数**
- `x` - 窗口左上角 X 坐标（屏幕坐标）
- `y` - 窗口左上角 Y 坐标（屏幕坐标）

**示例**
```csharp
// 移动到屏幕左上角
_windowService.SetWindowPosition(0, 0);

// 移动到屏幕中心
var screenWidth = SystemParameters.PrimaryScreenWidth;
var screenHeight = SystemParameters.PrimaryScreenHeight;
_windowService.SetWindowPosition(
    (screenWidth - windowWidth) / 2,
    (screenHeight - windowHeight) / 2
);
```

---

#### MoveToPresetPosition

移动到预设位置。

**签名**
```csharp
void MoveToPresetPosition(string position)
```

**参数**
- `position` - 预设位置名称
  - `"top-left"` - 左上角
  - `"top-right"` - 右上角
  - `"bottom-left"` - 左下角
  - `"bottom-right"` - 右下角
  - `"center"` - 屏幕中心

**示例**
```csharp
// 移动到右下角
_windowService.MoveToPresetPosition("bottom-right");

// 移动到中心
_windowService.MoveToPresetPosition("center");
```

---

#### SetWindowOpacity

设置窗口透明度。

**签名**
```csharp
void SetWindowOpacity(double opacity)
```

**参数**
- `opacity` - 透明度值（0.0 - 1.0）
  - 0.0：完全透明
  - 1.0：完全不透明
  - 应用中限制范围：0.05 - 0.4（5% - 40%）

**示例**
```csharp
// 设置为 20% 透明度
_windowService.SetWindowOpacity(0.2);

// 使用常量
using DesktopMemo.Core.Constants;
_windowService.SetWindowOpacity(WindowConstants.DEFAULT_TRANSPARENCY);
```

**注意事项**
- 建议使用 `WindowConstants` 和 `TransparencyHelper` 管理透明度
- 透明度过高会影响可读性

---

#### MinimizeToTray / RestoreFromTray

最小化到托盘或从托盘恢复。

**示例**
```csharp
// 最小化到托盘
_windowService.MinimizeToTray();

// 从托盘恢复
_windowService.RestoreFromTray();
```

---

## ITrayService

系统托盘服务，管理托盘图标和菜单。

### 接口定义

```csharp
namespace DesktopMemo.Core.Contracts;

public interface ITrayService : IDisposable
{
    // 事件
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

    // 方法
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

### 方法说明

#### Initialize

初始化托盘图标和菜单。

**示例**
```csharp
_trayService.Initialize();
_trayService.Show();
```

---

#### ShowBalloonTip

显示气泡提示。

**签名**
```csharp
void ShowBalloonTip(string title, string text, int timeout = 2000)
```

**参数**
- `title` - 提示标题
- `text` - 提示文本
- `timeout` - 显示时长（毫秒，默认 2000ms）

**示例**
```csharp
_trayService.ShowBalloonTip("保存成功", "备忘录已保存", 3000);
```

---

#### UpdateMenuTexts

更新托盘菜单文本（用于多语言切换）。

**示例**
```csharp
_trayService.UpdateMenuTexts(key => _localizationService[key]);
```

---

## ILocalizationService

多语言本地化服务。

### 接口定义

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

### 方法说明

#### 获取本地化字符串

**示例**
```csharp
// 使用索引器获取
var saveText = _localizationService["Save"];
var cancelText = _localizationService["Cancel"];

// 在 XAML 中使用
<TextBlock Text="{loc:Localize Save}" />
```

---

#### ChangeLanguage

切换语言。

**签名**
```csharp
void ChangeLanguage(string cultureName)
```

**参数**
- `cultureName` - 语言文化名称
  - `"zh-CN"` - 简体中文
  - `"en-US"` - 英语
  - `"zh-TW"` - 繁体中文
  - `"ja-JP"` - 日语
  - `"ko-KR"` - 韩语

**示例**
```csharp
// 切换到英语
_localizationService.ChangeLanguage("en-US");

// 订阅语言切换事件
_localizationService.LanguageChanged += (s, e) =>
{
    Console.WriteLine($"语言已切换: {e.OldCulture} -> {e.NewCulture}");
    // 更新UI
    _trayService.UpdateMenuTexts(key => _localizationService[key]);
};
```

---

#### GetSupportedLanguages

获取支持的语言列表。

**示例**
```csharp
var languages = _localizationService.GetSupportedLanguages();
foreach (var lang in languages)
{
    Console.WriteLine($"{lang.DisplayName} ({lang.Name})");
}
```

---

## ILogService

日志记录服务。

### 接口定义

```csharp
namespace DesktopMemo.Core.Contracts;

public interface ILogService
{
    void Log(string message, LogLevel level = LogLevel.Info);
    Task<IEnumerable<LogEntry>> GetLogsAsync(int count = 100);
    Task ClearLogsAsync();
}
```

### 方法说明

#### Log

记录日志。

**示例**
```csharp
_logService.Log("应用启动", LogLevel.Info);
_logService.Log("保存失败", LogLevel.Error);
```

---

## 实现类

### JsonSettingsService
- 位置：`DesktopMemo.Infrastructure.Services`
- 使用 JSON 格式存储设置
- 支持备份和恢复

### MemoSearchService
- 位置：`DesktopMemo.Infrastructure.Services`
- 支持普通文本和正则表达式搜索
- 不区分大小写选项

### WindowService
- 位置：`DesktopMemo.Infrastructure.Services`
- 使用 Win32 API 控制窗口
- 支持多种置顶模式和透明度

### TrayService
- 位置：`DesktopMemo.Infrastructure.Services`
- 使用 WinForms NotifyIcon 实现
- 提供丰富的托盘菜单

### LocalizationService
- 位置：`DesktopMemo.App.Localization`
- 基于 .resx 资源文件
- 支持动态语言切换

### FileLogService
- 位置：`DesktopMemo.Infrastructure.Services`
- 日志文件存储在 `.memodata/.logs/`
- 按日期自动轮转

---

## 最佳实践

### 1. 设置保存的原子性

```csharp
// ✅ 正确 - 原子性保存
try
{
    var newSettings = currentSettings with { Theme = AppTheme.Dark };
    await _settingsService.SaveAsync(newSettings);
    CurrentSettings = newSettings;
}
catch (Exception ex)
{
    Debug.WriteLine($"保存失败: {ex}");
}

// ❌ 错误 - 先更新内存，可能导致不一致
CurrentSettings = newSettings;
await _settingsService.SaveAsync(newSettings);
```

### 2. 搜索服务的使用

```csharp
// 查找并高亮显示
var matches = _searchService.FindMatches(content, keyword);
foreach (var match in matches)
{
    // 在编辑器中高亮显示该区域
    HighlightText(match.Index, match.Length);
}
```

### 3. 窗口服务的协调

```csharp
// 同时更新窗口状态和设置
_windowService.SetTopmostMode(TopmostMode.Always);
_trayService.UpdateTopmostState(TopmostMode.Always);

var newSettings = settings with { IsTopMost = true, IsDesktopMode = false };
await _settingsService.SaveAsync(newSettings);
```

---

## 相关文档

- [仓储接口文档](./Repositories.md)
- [领域模型文档](./Models.md)
- [枚举类型文档](./Enums.md)

---

**最后更新**：2025-11-15
