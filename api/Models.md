# 领域模型文档

领域模型定义了应用的核心数据结构，采用 C# Record 类型确保不可变性和线程安全。

---

## Memo

备忘录领域模型，表示一条完整的备忘录数据。

### 定义

```csharp
namespace DesktopMemo.Core.Models;

/// <summary>
/// 表示一条完整的备忘录数据。
/// </summary>
public sealed record Memo(
    Guid Id,
    string Title,
    string Content,
    string Preview,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<string> Tags,
    bool IsPinned,
    int Version = 1,
    SyncStatus SyncStatus = SyncStatus.Synced,
    DateTimeOffset? DeletedAt = null)
```

### 属性说明

| 属性 | 类型 | 说明 |
|-----|------|------|
| **Id** | `Guid` | 备忘录的唯一标识符 |
| **Title** | `string` | 备忘录标题（保留字段，当前未使用） |
| **Content** | `string` | 备忘录正文内容（Markdown 格式） |
| **Preview** | `string` | 预览文本（前 120 字符或第一行） |
| **CreatedAt** | `DateTimeOffset` | 创建时间 |
| **UpdatedAt** | `DateTimeOffset` | 最后更新时间 |
| **Tags** | `IReadOnlyList<string>` | 标签列表（保留字段，未来功能） |
| **IsPinned** | `bool` | 是否置顶 |
| **Version** | `int` | 版本号（用于乐观并发控制） |
| **SyncStatus** | `SyncStatus` | 同步状态（用于云同步功能） |
| **DeletedAt** | `DateTimeOffset?` | 删除时间（软删除标记） |

### 计算属性

#### DisplayTitle

获取用于显示的标题。

```csharp
public string DisplayTitle => GetDisplayTitle();
```

**逻辑**
1. 如果内容为空，使用 `Title` 字段
2. 否则使用内容的第一行作为标题
3. 如果第一行超过 50 字符，截断并添加 `...`

**示例**
```csharp
var memo = Memo.CreateNew("", "# 项目计划\n\n第一步...");
Console.WriteLine(memo.DisplayTitle); // 输出: # 项目计划

var memo2 = Memo.CreateNew("", "这是一个非常长的标题，超过了五十个字符的限制，因此会被截断");
Console.WriteLine(memo2.DisplayTitle); // 输出: 这是一个非常长的标题，超过了五十个字符的限制，因此会被截...
```

---

### 工厂方法

#### CreateNew

创建一个新的备忘录。

**签名**
```csharp
public static Memo CreateNew(string title, string content)
```

**参数**
- `title` - 标题（通常传空字符串）
- `content` - 内容

**返回值**
- 新创建的备忘录对象

**示例**
```csharp
var memo = Memo.CreateNew("", "我的第一条备忘录");
```

**特性**
- 自动生成 GUID
- 自动设置创建和更新时间为当前时间
- 自动生成预览文本
- 初始化为未置顶、未删除状态
- 版本号设为 1
- 同步状态设为 `Synced`（新建时为已同步状态）

---

### 更新方法

由于 Memo 是 Record 类型（不可变），所有更新操作都返回新的对象实例。

#### WithContent

更新备忘录内容。

**签名**
```csharp
public Memo WithContent(string content, DateTimeOffset timestamp)
```

**参数**
- `content` - 新的内容
- `timestamp` - 更新时间戳

**返回值**
- 包含新内容的备忘录副本

**示例**
```csharp
var updated = memo.WithContent("更新后的内容", DateTimeOffset.Now);
```

**自动处理**
- 更新 `Content` 字段
- 重新生成 `Preview`
- 更新 `UpdatedAt` 时间戳
- 版本号自动 +1
- 同步状态设为 `PendingSync`

---

#### WithMetadata

更新备忘录元数据（标题、标签、置顶状态）。

**签名**
```csharp
public Memo WithMetadata(string title, IReadOnlyList<string> tags, bool isPinned, DateTimeOffset timestamp)
```

**参数**
- `title` - 新标题
- `tags` - 新标签列表
- `isPinned` - 是否置顶
- `timestamp` - 更新时间戳

**返回值**
- 包含新元数据的备忘录副本

**示例**
```csharp
var tags = new List<string> { "工作", "重要" };
var updated = memo.WithMetadata("新标题", tags, isPinned: true, DateTimeOffset.Now);
```

**注意**：当前版本主要使用 `IsPinned`，标题和标签功能保留以供未来使用。

---

### 同步相关方法

这些方法为未来的云同步功能预留。

#### MarkAsSynced

标记为已同步状态。

**签名**
```csharp
public Memo MarkAsSynced(int remoteVersion)
```

**参数**
- `remoteVersion` - 远程服务器的版本号

**返回值**
- 已同步状态的备忘录副本

**示例**
```csharp
var synced = memo.MarkAsSynced(remoteVersion: 5);
```

---

#### MarkAsConflict

标记为冲突状态。

**签名**
```csharp
public Memo MarkAsConflict()
```

**示例**
```csharp
var conflicted = memo.MarkAsConflict();
```

---

### Preview 生成逻辑

Preview 字段自动从内容生成，规则如下：

1. 如果内容为空，返回空字符串
2. 如果内容 ≤ 120 字符，返回完整内容
3. 如果第一行 < 120 字符，返回第一行
4. 否则返回前 120 字符 + `...`

**示例**
```csharp
// 短内容
var memo1 = Memo.CreateNew("", "短内容");
Console.WriteLine(memo1.Preview); // 输出: 短内容

// 多行内容
var memo2 = Memo.CreateNew("", "第一行\n第二行\n第三行");
Console.WriteLine(memo2.Preview); // 输出: 第一行

// 长内容
var longContent = new string('A', 150);
var memo3 = Memo.CreateNew("", longContent);
Console.WriteLine(memo3.Preview.Length); // 输出: 123 (120 + "...")
```

---

## TodoItem

待办事项领域模型。

### 定义

```csharp
namespace DesktopMemo.Core.Models;

/// <summary>
/// 表示一个待办事项。
/// </summary>
public sealed record TodoItem(
    Guid Id,
    string Content,
    bool IsCompleted,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int SortOrder,
    int Version = 1,
    SyncStatus SyncStatus = SyncStatus.Synced,
    DateTimeOffset? CompletedAt = null,
    DateTimeOffset? DeletedAt = null)
```

### 属性说明

| 属性 | 类型 | 说明 |
|-----|------|------|
| **Id** | `Guid` | 待办事项的唯一标识符 |
| **Content** | `string` | 待办事项内容 |
| **IsCompleted** | `bool` | 是否已完成 |
| **CreatedAt** | `DateTimeOffset` | 创建时间 |
| **UpdatedAt** | `DateTimeOffset` | 最后更新时间 |
| **SortOrder** | `int` | 排序顺序（数字越小越靠前） |
| **Version** | `int` | 版本号 |
| **SyncStatus** | `SyncStatus` | 同步状态 |
| **CompletedAt** | `DateTimeOffset?` | 完成时间 |
| **DeletedAt** | `DateTimeOffset?` | 删除时间 |

---

### 工厂方法

#### CreateNew

创建一个新的待办事项。

**签名**
```csharp
public static TodoItem CreateNew(string content, int sortOrder = 0)
```

**参数**
- `content` - 待办事项内容
- `sortOrder` - 排序顺序（默认：0）

**示例**
```csharp
var todo = TodoItem.CreateNew("完成项目文档", sortOrder: 0);
```

**特性**
- 自动生成 GUID
- 初始状态为未完成
- 自动设置时间戳
- 同步状态设为 `PendingSync`

---

### 更新方法

#### ToggleCompleted

切换完成状态。

**签名**
```csharp
public TodoItem ToggleCompleted()
```

**返回值**
- 状态切换后的待办事项副本

**示例**
```csharp
var completed = todo.ToggleCompleted(); // 未完成 → 已完成
var uncompleted = completed.ToggleCompleted(); // 已完成 → 未完成
```

**自动处理**
- 切换 `IsCompleted` 状态
- 如果变为已完成，设置 `CompletedAt` 为当前时间
- 如果变为未完成，清除 `CompletedAt`
- 更新 `UpdatedAt` 时间戳
- 版本号 +1
- 同步状态设为 `PendingSync`

---

#### WithContent

更新待办内容。

**签名**
```csharp
public TodoItem WithContent(string content)
```

**示例**
```csharp
var updated = todo.WithContent("更新后的待办事项");
```

---

#### WithSortOrder

更新排序顺序。

**签名**
```csharp
public TodoItem WithSortOrder(int sortOrder)
```

**示例**
```csharp
var reordered = todo.WithSortOrder(5);
```

---

#### MarkAsSynced / MarkAsConflict

同步相关方法，用法与 Memo 相同。

---

## WindowSettings

窗口设置领域模型。

### 定义

```csharp
namespace DesktopMemo.Core.Models;

/// <summary>
/// 表示窗口相关的用户设置。
/// </summary>
public sealed record WindowSettings(
    double Width,
    double Height,
    double Left,
    double Top,
    double Transparency,
    bool IsTopMost,
    bool IsDesktopMode,
    bool IsClickThrough,
    bool ShowDeleteConfirmation = true,
    bool ShowExitConfirmation = true,
    bool DefaultExitToTray = true,
    string PreferredLanguage = "zh-CN",
    string CurrentPage = "memo",
    bool TodoInputVisible = true,
    AppTheme Theme = AppTheme.Light,
    bool IsWindowPinned = false)
```

### 属性说明

| 属性 | 类型 | 默认值 | 说明 |
|-----|------|--------|------|
| **Width** | `double` | 900 | 窗口宽度 |
| **Height** | `double` | 600 | 窗口高度 |
| **Left** | `double` | `NaN` | 窗口左侧位置 |
| **Top** | `double` | `NaN` | 窗口顶部位置 |
| **Transparency** | `double` | 0.05 | 透明度（0.05-0.4） |
| **IsTopMost** | `bool` | false | 是否总是置顶 |
| **IsDesktopMode** | `bool` | true | 是否桌面模式置顶 |
| **IsClickThrough** | `bool` | false | 是否启用穿透模式 |
| **ShowDeleteConfirmation** | `bool` | true | 删除时是否显示确认对话框 |
| **ShowExitConfirmation** | `bool` | true | 退出时是否显示确认对话框 |
| **DefaultExitToTray** | `bool` | true | 默认退出到托盘 |
| **PreferredLanguage** | `string` | "zh-CN" | 首选语言 |
| **CurrentPage** | `string` | "memo" | 当前页面（memo/todo） |
| **TodoInputVisible** | `bool` | true | 待办输入框是否可见 |
| **Theme** | `AppTheme` | `System` | 应用主题 |
| **IsWindowPinned** | `bool` | false | 窗口是否被固定 |

---

### 默认设置

```csharp
public static WindowSettings Default => new(
    900, 600,
    double.NaN, double.NaN,
    WindowConstants.DEFAULT_TRANSPARENCY,
    false, true, false,
    true, true, true,
    "zh-CN", "memo", true,
    AppTheme.System, false
);
```

**说明**
- `Left` 和 `Top` 为 `NaN` 表示窗口居中
- 默认启用桌面模式置顶
- 默认透明度为 5%
- 默认主题跟随系统

---

### 更新方法

#### WithLocation

更新窗口位置。

**签名**
```csharp
public WindowSettings WithLocation(double left, double top)
```

**示例**
```csharp
var newSettings = settings.WithLocation(100, 200);
```

---

#### WithSize

更新窗口大小。

**签名**
```csharp
public WindowSettings WithSize(double width, double height)
```

**示例**
```csharp
var newSettings = settings.WithSize(1200, 800);
```

---

#### WithAppearance

更新外观设置。

**签名**
```csharp
public WindowSettings WithAppearance(double transparency, bool topMost, bool desktopMode, bool clickThrough)
```

**示例**
```csharp
var newSettings = settings.WithAppearance(
    transparency: 0.2,
    topMost: true,
    desktopMode: false,
    clickThrough: false
);
```

---

### 使用 with 表达式

由于 WindowSettings 是 Record 类型，也可以直接使用 `with` 表达式：

```csharp
// 更新单个属性
var newSettings = settings with { Theme = AppTheme.Dark };

// 更新多个属性
var newSettings = settings with
{
    Width = 1200,
    Height = 800,
    Theme = AppTheme.Dark,
    Transparency = 0.15
};
```

---

## LogEntry

日志条目模型（简化版）。

### 定义

```csharp
namespace DesktopMemo.Core.Models;

public sealed record LogEntry(
    DateTimeOffset Timestamp,
    LogLevel Level,
    string Message,
    string? Exception = null)
```

### 属性说明

| 属性 | 类型 | 说明 |
|-----|------|------|
| **Timestamp** | `DateTimeOffset` | 日志时间戳 |
| **Level** | `LogLevel` | 日志级别 |
| **Message** | `string` | 日志消息 |
| **Exception** | `string?` | 异常信息（可选） |

---

## 最佳实践

### 1. 使用工厂方法创建

```csharp
// ✅ 正确 - 使用工厂方法
var memo = Memo.CreateNew("", "内容");
var todo = TodoItem.CreateNew("待办事项");

// ❌ 错误 - 手动构造
var memo = new Memo(Guid.NewGuid(), "", "内容", ...); // 容易出错
```

---

### 2. 使用 with 表达式更新

```csharp
// ✅ 正确 - 使用 with 表达式或专用方法
var updated = memo.WithContent("新内容", DateTimeOffset.Now);
var updated2 = settings with { Theme = AppTheme.Dark };

// ❌ 错误 - 尝试直接修改（编译错误，Record 是不可变的）
memo.Content = "新内容"; // 编译错误
```

---

### 3. 检查可空字段

```csharp
// 检查删除状态
if (memo.DeletedAt.HasValue)
{
    Console.WriteLine($"已于 {memo.DeletedAt.Value} 删除");
}

// 检查完成时间
if (todo.CompletedAt.HasValue)
{
    var duration = todo.CompletedAt.Value - todo.CreatedAt;
    Console.WriteLine($"耗时: {duration.TotalHours} 小时");
}
```

---

### 4. 版本控制和冲突检测

```csharp
// 乐观并发控制
public async Task UpdateMemoAsync(Memo updatedMemo)
{
    var current = await _repository.GetByIdAsync(updatedMemo.Id);

    if (current == null)
    {
        throw new InvalidOperationException("备忘录不存在");
    }

    if (current.Version != updatedMemo.Version)
    {
        throw new InvalidOperationException("版本冲突，数据已被其他操作修改");
    }

    await _repository.UpdateAsync(updatedMemo);
}
```

---

### 5. 使用只读集合

```csharp
// ✅ 正确 - 使用只读集合
var tags = new List<string> { "工作", "重要" }.AsReadOnly();
var memo = memo.WithMetadata("标题", tags, true, DateTimeOffset.Now);

// ❌ 错误 - 传递可变集合（可能导致意外修改）
var tags = new List<string> { "工作", "重要" };
var memo = memo.WithMetadata("标题", tags, true, DateTimeOffset.Now);
tags.Add("紧急"); // 这会影响已创建的 memo
```

---

## 不可变性的优势

### 1. 线程安全

```csharp
// 多个线程可以安全地读取同一个 Memo 对象
var memo = Memo.CreateNew("", "内容");

Task.Run(() => Console.WriteLine(memo.Content)); // 线程1
Task.Run(() => Console.WriteLine(memo.Content)); // 线程2
// 无需加锁，完全安全
```

---

### 2. 历史追踪

```csharp
// 保留历史版本
var history = new List<Memo>();
var current = Memo.CreateNew("", "初始内容");
history.Add(current);

current = current.WithContent("版本2", DateTimeOffset.Now);
history.Add(current);

current = current.WithContent("版本3", DateTimeOffset.Now);
history.Add(current);

// 可以轻松访问任何历史版本
Console.WriteLine(history[0].Content); // 初始内容
Console.WriteLine(history[1].Content); // 版本2
```

---

### 3. 简化状态管理

```csharp
// 使用 with 表达式创建修改后的副本
var darkSettings = settings with { Theme = AppTheme.Dark };

// 原始对象不受影响
Console.WriteLine(settings.Theme);     // Light
Console.WriteLine(darkSettings.Theme); // Dark
```

---

## 相关文档

- [枚举类型文档](./Enums.md)
- [仓储接口文档](./Repositories.md)
- [服务接口文档](./Services.md)

---

**最后更新**：2025-11-15
