# 枚举类型文档

本文档描述 DesktopMemo 应用中使用的所有枚举类型。

---

## TopmostMode

窗口置顶模式枚举。

### 定义

```csharp
namespace DesktopMemo.Core.Contracts;

/// <summary>
/// 窗口置顶模式
/// </summary>
public enum TopmostMode
{
    /// <summary>
    /// 普通模式，不置顶
    /// </summary>
    Normal = 0,

    /// <summary>
    /// 桌面层面置顶（在桌面图标之上，其他窗口之下）
    /// </summary>
    Desktop = 1,

    /// <summary>
    /// 总是置顶（在所有窗口之上）
    /// </summary>
    Always = 2
}
```

### 值说明

| 值 | 名称 | 描述 | 使用场景 |
|---|------|------|---------|
| 0 | **Normal** | 普通窗口，不置顶 | 与其他窗口平等，可以被其他窗口遮挡 |
| 1 | **Desktop** | 桌面层置顶 | 显示在桌面图标之上，但在其他应用窗口之下（默认模式） |
| 2 | **Always** | 总是置顶 | 始终显示在所有窗口最上层，即使切换到其他应用 |

---

### 使用示例

#### 设置置顶模式

```csharp
// 设置为普通模式
_windowService.SetTopmostMode(TopmostMode.Normal);

// 设置为桌面置顶
_windowService.SetTopmostMode(TopmostMode.Desktop);

// 设置为总是置顶
_windowService.SetTopmostMode(TopmostMode.Always);
```

---

#### 在托盘菜单中切换

```csharp
_trayService.TopmostModeChangeClick += (sender, mode) =>
{
    _windowService.SetTopmostMode(mode);
    _trayService.UpdateTopmostState(mode);
};
```

---

#### 从设置恢复

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

### 实现细节

#### Normal 模式
- WPF: `window.Topmost = false`
- Win32: 不调用特殊 API

#### Desktop 模式
- Win32 API: `SetWindowPos(hwnd, HWND_BOTTOM, ...)`
- 将窗口放置在 Z 轴的桌面层

#### Always 模式
- WPF: `window.Topmost = true`
- 窗口始终在 Z 轴顶部

---

### 注意事项

- **Desktop** 模式是默认推荐模式，既能保持便签可见，又不干扰其他应用
- **Always** 模式适合需要随时查看的重要信息
- **Normal** 模式适合临时查看后可以切换到后台的场景
- 模式切换后需要同步更新托盘菜单状态

---

## AppTheme

应用主题枚举。

### 定义

```csharp
namespace DesktopMemo.Core.Models;

/// <summary>
/// 应用主题类型
/// </summary>
public enum AppTheme
{
    /// <summary>
    /// 亮色主题
    /// </summary>
    Light = 0,

    /// <summary>
    /// 暗色主题
    /// </summary>
    Dark = 1,

    /// <summary>
    /// 跟随系统主题
    /// </summary>
    System = 2
}
```

### 值说明

| 值 | 名称 | 描述 |
|---|------|------|
| 0 | **Light** | 亮色主题，白色背景 |
| 1 | **Dark** | 暗色主题，深色背景 |
| 2 | **System** | 跟随系统主题设置（Windows 10/11） |

---

### 使用示例

#### 切换主题

```csharp
// 切换到暗色主题
var newSettings = settings with { Theme = AppTheme.Dark };
await _settingsService.SaveAsync(newSettings);
ApplyTheme(AppTheme.Dark);

// 切换到亮色主题
var newSettings = settings with { Theme = AppTheme.Light };
await _settingsService.SaveAsync(newSettings);
ApplyTheme(AppTheme.Light);

// 跟随系统
var newSettings = settings with { Theme = AppTheme.System };
await _settingsService.SaveAsync(newSettings);
ApplyTheme(AppTheme.System);
```

---

#### 应用主题到 UI

```csharp
private void ApplyTheme(AppTheme theme)
{
    var lightTheme = new Uri("pack://application:,,,/Resources/Themes/Light.xaml");
    var darkTheme = new Uri("pack://application:,,,/Resources/Themes/Dark.xaml");

    var resources = Application.Current.Resources.MergedDictionaries;

    // 移除现有主题
    var existingTheme = resources.FirstOrDefault(d =>
        d.Source == lightTheme || d.Source == darkTheme);
    if (existingTheme != null)
    {
        resources.Remove(existingTheme);
    }

    // 应用新主题
    Uri themeUri;
    if (theme == AppTheme.System)
    {
        // 检测系统主题
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

#### 检测系统主题

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
        return "Light"; // 默认亮色
    }
}
```

---

### 主题资源文件

#### Light.xaml
```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- 亮色主题颜色 -->
    <SolidColorBrush x:Key="PrimaryBackgroundBrush" Color="#FFFFFF"/>
    <SolidColorBrush x:Key="PrimaryForegroundBrush" Color="#000000"/>
    <SolidColorBrush x:Key="SecondaryBackgroundBrush" Color="#F5F5F5"/>
    <!-- 更多颜色定义... -->

</ResourceDictionary>
```

#### Dark.xaml
```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- 暗色主题颜色 -->
    <SolidColorBrush x:Key="PrimaryBackgroundBrush" Color="#1E1E1E"/>
    <SolidColorBrush x:Key="PrimaryForegroundBrush" Color="#FFFFFF"/>
    <SolidColorBrush x:Key="SecondaryBackgroundBrush" Color="#2D2D2D"/>
    <!-- 更多颜色定义... -->

</ResourceDictionary>
```

---

### 注意事项

- 主题切换后需要刷新所有 UI 元素
- 使用动态资源引用确保主题能够实时更新
- System 模式需要监听 Windows 主题变化事件

---

## SyncStatus

数据同步状态枚举，用于云同步功能（计划中）。

### 定义

```csharp
namespace DesktopMemo.Core.Models;

/// <summary>
/// 数据同步状态枚举
/// </summary>
public enum SyncStatus
{
    /// <summary>
    /// 已同步到云端
    /// </summary>
    Synced = 0,

    /// <summary>
    /// 等待同步到云端
    /// </summary>
    PendingSync = 1,

    /// <summary>
    /// 存在同步冲突
    /// </summary>
    Conflict = 2
}
```

### 值说明

| 值 | 名称 | 描述 | UI 显示 |
|---|------|------|---------|
| 0 | **Synced** | 本地数据与云端一致 | ✓ 绿色勾选 |
| 1 | **PendingSync** | 本地有修改，等待上传 | ⏱ 等待图标 |
| 2 | **Conflict** | 本地和云端都有修改，需要解决冲突 | ⚠ 警告图标 |

---

### 使用示例

#### 检查同步状态

```csharp
public string GetSyncStatusText(SyncStatus status)
{
    return status switch
    {
        SyncStatus.Synced => "已同步",
        SyncStatus.PendingSync => "等待同步",
        SyncStatus.Conflict => "冲突",
        _ => "未知"
    };
}
```

---

#### 根据状态显示图标

```xaml
<DataTemplate x:Key="SyncStatusTemplate">
    <StackPanel Orientation="Horizontal">
        <!-- 同步状态图标 -->
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

#### 同步流程

```csharp
public async Task SyncMemoAsync(Memo memo)
{
    if (memo.SyncStatus == SyncStatus.Synced)
    {
        // 已同步，无需操作
        return;
    }

    if (memo.SyncStatus == SyncStatus.Conflict)
    {
        // 需要解决冲突
        await ResolveConflictAsync(memo);
        return;
    }

    // 上传到云端
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

### 状态转换

```
CreateNew
   ↓
Synced (初始状态)
   ↓ (本地修改)
PendingSync
   ↓ (上传成功)
Synced
   ↓ (检测到冲突)
Conflict
   ↓ (解决冲突)
Synced
```

---

### 注意事项

- 当前版本（v2.3.0）云同步功能尚未实现，但数据模型已准备就绪
- 新建数据默认为 `Synced` 状态
- 任何本地修改都会自动标记为 `PendingSync`
- 冲突需要用户手动解决

---

## LogLevel

日志级别枚举。

### 定义

```csharp
namespace DesktopMemo.Core.Models;

/// <summary>
/// 日志级别
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// 调试信息
    /// </summary>
    Debug = 0,

    /// <summary>
    /// 一般信息
    /// </summary>
    Info = 1,

    /// <summary>
    /// 警告信息
    /// </summary>
    Warning = 2,

    /// <summary>
    /// 错误信息
    /// </summary>
    Error = 3,

    /// <summary>
    /// 致命错误
    /// </summary>
    Fatal = 4
}
```

### 值说明

| 值 | 名称 | 描述 | 使用场景 |
|---|------|------|---------|
| 0 | **Debug** | 调试信息 | 开发时的详细追踪信息 |
| 1 | **Info** | 一般信息 | 正常操作流程记录 |
| 2 | **Warning** | 警告信息 | 潜在问题，不影响功能 |
| 3 | **Error** | 错误信息 | 操作失败，需要关注 |
| 4 | **Fatal** | 致命错误 | 严重错误，可能导致崩溃 |

---

### 使用示例

#### 记录不同级别的日志

```csharp
// 调试信息
_logService.Log("开始加载备忘录", LogLevel.Debug);

// 一般信息
_logService.Log("应用启动成功", LogLevel.Info);

// 警告
_logService.Log("设置文件损坏，使用备份恢复", LogLevel.Warning);

// 错误
_logService.Log("保存备忘录失败: 权限不足", LogLevel.Error);

// 致命错误
_logService.Log("数据库连接失败，应用无法继续", LogLevel.Fatal);
```

---

#### 根据级别过滤日志

```csharp
public async Task<IEnumerable<LogEntry>> GetErrorLogsAsync()
{
    var allLogs = await _logService.GetLogsAsync();
    return allLogs.Where(log => log.Level >= LogLevel.Error);
}
```

---

#### 根据级别设置颜色

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

### 日志级别的使用原则

#### Debug
```csharp
// 详细的执行流程
_logService.Log($"进入方法: LoadMemosAsync, 参数: count={count}", LogLevel.Debug);
_logService.Log($"SQL查询: {query}", LogLevel.Debug);
```

#### Info
```csharp
// 重要的业务操作
_logService.Log("备忘录创建成功", LogLevel.Info);
_logService.Log("设置保存成功", LogLevel.Info);
```

#### Warning
```csharp
// 异常情况，但有备用方案
_logService.Log("无法加载用户设置，使用默认设置", LogLevel.Warning);
_logService.Log("数据迁移部分失败，已跳过", LogLevel.Warning);
```

#### Error
```csharp
// 操作失败
_logService.Log($"保存备忘录失败: {ex.Message}", LogLevel.Error);
_logService.Log("文件系统访问被拒绝", LogLevel.Error);
```

#### Fatal
```csharp
// 致命错误，应用可能无法继续
_logService.Log("数据库初始化失败，应用无法启动", LogLevel.Fatal);
_logService.Log($"未处理的异常: {ex}", LogLevel.Fatal);
```

---

## 枚举转换工具

### TopmostMode 与 Settings 的转换

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

**使用示例**
```csharp
// 从设置转换到模式
var mode = TopmostModeExtensions.FromSettings(
    settings.IsTopMost,
    settings.IsDesktopMode
);

// 从模式转换到设置
var (isTopMost, isDesktopMode) = mode.ToSettings();
var newSettings = settings with
{
    IsTopMost = isTopMost,
    IsDesktopMode = isDesktopMode
};
```

---

### AppTheme 本地化

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

**资源文件 (Strings.resx)**
```xml
<data name="Theme_Light" xml:space="preserve">
    <value>浅色</value>
</data>
<data name="Theme_Dark" xml:space="preserve">
    <value>深色</value>
</data>
<data name="Theme_System" xml:space="preserve">
    <value>跟随系统</value>
</data>
```

---

## 最佳实践

### 1. 使用明确的枚举值

```csharp
// ✅ 正确 - 使用枚举名称
_windowService.SetTopmostMode(TopmostMode.Desktop);

// ❌ 错误 - 使用魔法数字
_windowService.SetTopmostMode((TopmostMode)1);
```

---

### 2. 处理未知枚举值

```csharp
// ✅ 正确 - 使用 switch 表达式并提供默认值
var text = status switch
{
    SyncStatus.Synced => "已同步",
    SyncStatus.PendingSync => "等待同步",
    SyncStatus.Conflict => "冲突",
    _ => "未知状态" // 处理未来可能添加的值
};

// ❌ 错误 - 没有默认分支
var text = status switch
{
    SyncStatus.Synced => "已同步",
    SyncStatus.PendingSync => "等待同步",
    SyncStatus.Conflict => "冲突"
    // 如果添加新值会抛出异常
};
```

---

### 3. 枚举序列化

```csharp
// JSON 序列化为字符串（推荐）
var options = new JsonSerializerOptions
{
    Converters = { new JsonStringEnumConverter() }
};

var json = JsonSerializer.Serialize(settings, options);
// 结果: { "Theme": "Dark" } 而不是 { "Theme": 1 }
```

---

## 相关文档

- [领域模型文档](./Models.md)
- [服务接口文档](./Services.md)
- [仓储接口文档](./Repositories.md)

---

**最后更新**：2025-11-15
