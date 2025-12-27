# DesktopMemo API 文档

本目录包含 DesktopMemo 项目的完整 API 文档，详细介绍了核心接口、模型和服务的使用方法。

---

## 📚 文档目录

### [1. 仓储接口 (Repositories)](./Repositories.md)
提供数据持久化访问的核心接口：
- **IMemoRepository** - 备忘录数据访问
- **ITodoRepository** - 待办事项数据访问

### [2. 服务接口 (Services)](./Services.md)
提供业务逻辑和系统功能的服务接口：
- **ISettingsService** - 应用设置管理
- **IMemoSearchService** - 备忘录搜索
- **IWindowService** - 窗口管理（置顶、透明度、穿透等）
- **ITrayService** - 系统托盘服务
- **ILocalizationService** - 多语言本地化服务
- **ILogService** - 日志记录服务

### [3. 领域模型 (Models)](./Models.md)
定义应用核心数据结构：
- **Memo** - 备忘录模型
- **TodoItem** - 待办事项模型
- **WindowSettings** - 窗口设置模型
- **LogEntry** - 日志条目模型

### [4. 枚举类型 (Enums)](./Enums.md)
定义应用中使用的枚举类型：
- **TopmostMode** - 窗口置顶模式
- **AppTheme** - 应用主题
- **SyncStatus** - 同步状态
- **LogLevel** - 日志级别

---

## 🎯 快速开始

### 依赖注入配置

DesktopMemo 使用 Microsoft.Extensions.DependencyInjection 进行依赖注入配置。核心服务在 `src/DesktopMemo.App/App.xaml.cs` 中注册（部分实现需要 `dataDirectory` 参数）。

```csharp
// App.xaml.cs
private void ConfigureServices(IServiceCollection services)
{
    // 仓储
    services.AddSingleton<IMemoRepository>(_ => new SqliteIndexedMemoRepository(dataDirectory));
    services.AddSingleton<ITodoRepository>(_ => new SqliteTodoRepository(dataDirectory));

    // 服务
    services.AddSingleton<ISettingsService>(_ => new JsonSettingsService(dataDirectory));
    services.AddSingleton<IMemoSearchService, MemoSearchService>();
    services.AddSingleton<IWindowService, WindowService>();
    services.AddSingleton<ITrayService, TrayService>();
    services.AddSingleton<ILocalizationService, LocalizationService>();
    services.AddSingleton<ILogService>(_ => new FileLogService(dataDirectory));

    // ViewModels
    services.AddTransient<MainViewModel>();
}
```

### 在 ViewModel 中使用

```csharp
public class MainViewModel : ObservableObject
{
    private readonly IMemoRepository _memoRepository;
    private readonly ISettingsService _settingsService;

    public MainViewModel(
        IMemoRepository memoRepository,
        ISettingsService settingsService)
    {
        _memoRepository = memoRepository;
        _settingsService = settingsService;
    }

    public async Task LoadMemosAsync()
    {
        var memos = await _memoRepository.GetAllAsync();
        // 处理备忘录列表
    }
}
```

---

## 🏗️ 架构设计

### 三层架构

```
┌─────────────────────────────────────────┐
│  表示层 (DesktopMemo.App)               │
│  - ViewModels 使用接口                  │
│  - 不直接依赖实现类                      │
├─────────────────────────────────────────┤
│  领域层 (DesktopMemo.Core)              │
│  - 定义接口 (Contracts)                 │
│  - 定义模型 (Models)                    │
│  - 纯 .NET 库，无外部依赖                │
├─────────────────────────────────────────┤
│  基础设施层 (DesktopMemo.Infrastructure)│
│  - 实现接口                             │
│  - 处理数据访问和外部系统交互            │
└─────────────────────────────────────────┘
```

### 依赖方向

- **表示层** → **领域层**（依赖接口）
- **基础设施层** → **领域层**（实现接口）
- **领域层** 不依赖任何其他层（核心）

---

## 🔄 数据流程

### 备忘录 CRUD 流程

```
用户操作 (View)
    ↓
命令触发 (ViewModel)
    ↓
调用接口方法 (IMemoRepository)
    ↓
实现类处理 (SqliteIndexedMemoRepository)
    ↓
数据持久化 (SQLite + Markdown)
    ↓
更新 ViewModel 状态
    ↓
UI 更新 (Data Binding)
```

### 设置保存流程

```
用户修改设置
    ↓
ViewModel 更新状态
    ↓
调用 ISettingsService.SaveAsync()
    ↓
异步保存到 JSON 文件
    ↓
成功后更新内存状态
```

---

## 🧩 关键设计模式

### 1. Repository 模式

所有数据访问通过仓储接口进行，隔离数据访问逻辑。

```csharp
public interface IMemoRepository
{
    Task<IReadOnlyList<Memo>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Memo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Memo memo, CancellationToken cancellationToken = default);
    Task UpdateAsync(Memo memo, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
```

### 2. Service 模式

业务逻辑和系统功能封装为服务接口。

```csharp
public interface ISettingsService
{
    Task<WindowSettings> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(WindowSettings settings, CancellationToken cancellationToken = default);
}
```

### 3. Immutable Record

使用 C# 9+ 的 Record 类型确保数据不可变性。

```csharp
public sealed record Memo(
    Guid Id,
    string Title,
    string Content,
    // ...
)
{
    public Memo WithContent(string content, DateTimeOffset timestamp) => this with
    {
        Content = content,
        UpdatedAt = timestamp
    };
}
```

---

## 📝 命名约定

### 接口命名

- 所有接口以 `I` 开头
- 使用 PascalCase
- 仓储接口以 `Repository` 结尾
- 服务接口以 `Service` 结尾

示例：`IMemoRepository`、`ISettingsService`

### 方法命名

- 异步方法以 `Async` 结尾
- 使用动词开头
- 清晰描述操作意图

示例：`GetAllAsync`、`SaveAsync`、`UpdateAsync`

### 模型命名

- 使用名词
- PascalCase
- 描述业务概念

示例：`Memo`、`TodoItem`、`WindowSettings`

---

## ⚡ 异步编程规范

### 所有 I/O 操作必须异步

```csharp
// ✅ 正确 - 异步操作
public async Task<IReadOnlyList<Memo>> GetAllAsync(CancellationToken cancellationToken = default)
{
    return await database.QueryAsync<Memo>("SELECT * FROM Memos");
}

// ❌ 错误 - 同步操作
public IReadOnlyList<Memo> GetAll()
{
    return database.Query<Memo>("SELECT * FROM Memos");
}
```

### 支持取消令牌

```csharp
public async Task<Memo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
{
    return await database.QueryFirstOrDefaultAsync<Memo>(
        "SELECT * FROM Memos WHERE Id = @Id",
        new { Id = id },
        cancellationToken);
}
```

### UI 线程与后台线程切换

```csharp
// 在 ViewModel 中调用异步方法
await Task.Run(async () =>
{
    // 后台线程执行 I/O 操作
    var settings = await _settingsService.LoadAsync();

    // 切换回 UI 线程更新界面
    await Application.Current.Dispatcher.InvokeAsync(() =>
    {
        Settings = settings;
    });
});
```

---

## 🔒 错误处理

### 异常层级处理

```csharp
try
{
    await _memoRepository.AddAsync(memo);
}
catch (InvalidOperationException ex)
{
    // 级别1：操作状态异常 - 用户友好提示
    SetStatus($"操作无效: {ex.Message}");
}
catch (UnauthorizedAccessException ex)
{
    // 级别2：权限异常 - 明确权限问题
    SetStatus($"权限不足: {ex.Message}");
}
catch (IOException ex)
{
    // 级别3：IO异常 - 文件系统问题
    SetStatus($"文件操作失败: {ex.Message}");
}
catch (Exception ex)
{
    // 级别4：未知异常 - 记录详细信息
    Debug.WriteLine($"未知异常: {ex}");
    SetStatus("操作失败，请稍后重试");
}
```

---

## 🧪 单元测试指南

### 接口隔离便于测试

由于所有依赖都通过接口注入，可以轻松创建 Mock 对象进行单元测试。

```csharp
// 使用 Moq 创建 Mock 对象
var mockMemoRepository = new Mock<IMemoRepository>();
mockMemoRepository
    .Setup(r => r.GetAllAsync(default))
    .ReturnsAsync(new List<Memo> { /* 测试数据 */ });

var viewModel = new MainViewModel(mockMemoRepository.Object);
await viewModel.LoadMemosAsync();

// 验证结果
Assert.AreEqual(expectedCount, viewModel.Memos.Count);
```

---

## 📖 扩展阅读

- [项目架构文档](../project_structure/README.md)
- [数据流和通信](../project_structure/04_数据流和通信.md)
- [贡献指南](../CONTRIBUTING.md)

---

## 📧 问题反馈

如果您在使用 API 时遇到问题或有改进建议：

- [提交 Issue](https://github.com/SaltedDoubao/DesktopMemo/issues)
- [参与讨论](https://github.com/SaltedDoubao/DesktopMemo/discussions)
- [查看源代码](https://github.com/SaltedDoubao/DesktopMemo/tree/main/src)

---

**最后更新**：2025-11-15
