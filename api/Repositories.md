# 仓储接口文档

仓储接口提供数据持久化访问的核心功能，遵循 Repository 模式，隔离数据访问逻辑。

---

## IMemoRepository

备忘录数据访问接口，提供备忘录的 CRUD 操作。

### 接口定义

```csharp
namespace DesktopMemo.Core.Contracts;

/// <summary>
/// 定义备忘录持久化访问的契约。
/// </summary>
public interface IMemoRepository
{
    Task<IReadOnlyList<Memo>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Memo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Memo memo, CancellationToken cancellationToken = default);
    Task UpdateAsync(Memo memo, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
```

### 方法说明

#### GetAllAsync

获取所有备忘录列表。

**签名**
```csharp
Task<IReadOnlyList<Memo>> GetAllAsync(CancellationToken cancellationToken = default)
```

**参数**
- `cancellationToken` - 取消令牌，用于取消异步操作

**返回值**
- `Task<IReadOnlyList<Memo>>` - 包含所有备忘录的只读列表

**示例**
```csharp
var memos = await _memoRepository.GetAllAsync();
foreach (var memo in memos)
{
    Console.WriteLine($"{memo.Title}: {memo.Preview}");
}
```

**注意事项**
- 返回的列表是只读的，不能修改
- 备忘录按更新时间倒序排列（最新的在前）
- 置顶备忘录会排在前面

---

#### GetByIdAsync

根据 ID 获取指定备忘录。

**签名**
```csharp
Task<Memo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
```

**参数**
- `id` - 备忘录的唯一标识符
- `cancellationToken` - 取消令牌

**返回值**
- `Task<Memo?>` - 找到的备忘录，如果不存在则返回 `null`

**示例**
```csharp
var memoId = Guid.Parse("...");
var memo = await _memoRepository.GetByIdAsync(memoId);

if (memo != null)
{
    Console.WriteLine($"找到备忘录: {memo.Title}");
}
else
{
    Console.WriteLine("备忘录不存在");
}
```

**注意事项**
- 如果 ID 不存在，返回 `null` 而不是抛出异常
- 建议在使用前检查返回值是否为 `null`

---

#### AddAsync

添加新的备忘录。

**签名**
```csharp
Task AddAsync(Memo memo, CancellationToken cancellationToken = default)
```

**参数**
- `memo` - 要添加的备忘录对象
- `cancellationToken` - 取消令牌

**返回值**
- `Task` - 异步任务

**示例**
```csharp
var newMemo = Memo.CreateNew("我的标题", "备忘录内容");
await _memoRepository.AddAsync(newMemo);
```

**注意事项**
- 备忘录会同时保存到 SQLite 数据库和 Markdown 文件
- 如果 ID 已存在，会抛出异常
- 建议使用 `Memo.CreateNew()` 创建新备忘录，确保正确初始化

**可能的异常**
- `InvalidOperationException` - 备忘录 ID 已存在
- `IOException` - 文件系统操作失败
- `UnauthorizedAccessException` - 没有文件写入权限

---

#### UpdateAsync

更新现有备忘录。

**签名**
```csharp
Task UpdateAsync(Memo memo, CancellationToken cancellationToken = default)
```

**参数**
- `memo` - 要更新的备忘录对象（包含新的数据）
- `cancellationToken` - 取消令牌

**返回值**
- `Task` - 异步任务

**示例**
```csharp
var existingMemo = await _memoRepository.GetByIdAsync(memoId);
if (existingMemo != null)
{
    var updatedMemo = existingMemo.WithContent(
        "新的内容",
        DateTimeOffset.Now
    );
    await _memoRepository.UpdateAsync(updatedMemo);
}
```

**注意事项**
- 使用 `with` 表达式创建新的备忘录对象（Record 类型特性）
- 更新会同时修改数据库和 Markdown 文件
- 版本号会自动递增
- 同步状态会自动设置为 `PendingSync`

**可能的异常**
- `InvalidOperationException` - 备忘录不存在
- `IOException` - 文件系统操作失败
- `UnauthorizedAccessException` - 没有文件写入权限

---

#### DeleteAsync

删除指定备忘录。

**签名**
```csharp
Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
```

**参数**
- `id` - 要删除的备忘录 ID
- `cancellationToken` - 取消令牌

**返回值**
- `Task` - 异步任务

**示例**
```csharp
await _memoRepository.DeleteAsync(memoId);
```

**注意事项**
- 会同时删除数据库记录和 Markdown 文件
- 软删除：实际上是设置 `DeletedAt` 字段，数据仍保留（用于云同步）
- 如果 ID 不存在，不会抛出异常（幂等操作）

**可能的异常**
- `IOException` - 文件系统操作失败
- `UnauthorizedAccessException` - 没有文件删除权限

---

## ITodoRepository

待办事项数据访问接口，提供待办事项的 CRUD 操作。

### 接口定义

```csharp
namespace DesktopMemo.Core.Contracts;

/// <summary>
/// 定义待办事项持久化访问的契约。
/// </summary>
public interface ITodoRepository
{
    Task<IReadOnlyList<TodoItem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TodoItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(TodoItem todoItem, CancellationToken cancellationToken = default);
    Task UpdateAsync(TodoItem todoItem, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
```

### 方法说明

#### GetAllAsync

获取所有待办事项列表。

**签名**
```csharp
Task<IReadOnlyList<TodoItem>> GetAllAsync(CancellationToken cancellationToken = default)
```

**参数**
- `cancellationToken` - 取消令牌

**返回值**
- `Task<IReadOnlyList<TodoItem>>` - 包含所有待办事项的只读列表

**示例**
```csharp
var todos = await _todoRepository.GetAllAsync();
var pendingTodos = todos.Where(t => !t.IsCompleted).ToList();
var completedTodos = todos.Where(t => t.IsCompleted).ToList();
```

**注意事项**
- 返回的列表按 `SortOrder` 排序
- 未完成的待办事项排在已完成的前面
- 已删除的待办事项不会返回

---

#### GetByIdAsync

根据 ID 获取指定待办事项。

**签名**
```csharp
Task<TodoItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
```

**参数**
- `id` - 待办事项的唯一标识符
- `cancellationToken` - 取消令牌

**返回值**
- `Task<TodoItem?>` - 找到的待办事项，如果不存在则返回 `null`

**示例**
```csharp
var todo = await _todoRepository.GetByIdAsync(todoId);
if (todo != null)
{
    Console.WriteLine($"待办: {todo.Content}, 状态: {(todo.IsCompleted ? "已完成" : "待完成")}");
}
```

---

#### AddAsync

添加新的待办事项。

**签名**
```csharp
Task AddAsync(TodoItem todoItem, CancellationToken cancellationToken = default)
```

**参数**
- `todoItem` - 要添加的待办事项对象
- `cancellationToken` - 取消令牌

**返回值**
- `Task` - 异步任务

**示例**
```csharp
var newTodo = TodoItem.CreateNew("完成项目文档", sortOrder: 0);
await _todoRepository.AddAsync(newTodo);
```

**注意事项**
- 建议使用 `TodoItem.CreateNew()` 创建新待办事项
- 新建的待办事项默认状态为未完成
- `SortOrder` 用于排序，数字越小越靠前

---

#### UpdateAsync

更新现有待办事项。

**签名**
```csharp
Task UpdateAsync(TodoItem todoItem, CancellationToken cancellationToken = default)
```

**参数**
- `todoItem` - 要更新的待办事项对象
- `cancellationToken` - 取消令牌

**返回值**
- `Task` - 异步任务

**示例**
```csharp
var todo = await _todoRepository.GetByIdAsync(todoId);
if (todo != null)
{
    // 切换完成状态
    var updatedTodo = todo.ToggleCompleted();
    await _todoRepository.UpdateAsync(updatedTodo);
}
```

**常用更新方法**
```csharp
// 切换完成状态
var updated = todo.ToggleCompleted();

// 更新内容
var updated = todo.WithContent("新的待办内容");

// 更新排序
var updated = todo.WithSortOrder(5);
```

---

#### DeleteAsync

删除指定待办事项。

**签名**
```csharp
Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
```

**参数**
- `id` - 要删除的待办事项 ID
- `cancellationToken` - 取消令牌

**返回值**
- `Task` - 异步任务

**示例**
```csharp
await _todoRepository.DeleteAsync(todoId);
```

**注意事项**
- 软删除：设置 `DeletedAt` 字段，数据仍保留
- 幂等操作，重复删除不会报错

---

## 实现类

### SqliteIndexedMemoRepository

位于 `DesktopMemo.Infrastructure` 项目中，使用 SQLite + Markdown 双存储实现。

**特性**
- SQLite FTS5 全文搜索索引
- Markdown 文件存储完整内容
- 支持事务操作
- 自动版本控制

**数据存储位置**
- 数据库：`.memodata/memos.db`
- 文件：`.memodata/content/{memoId}.md`

### SqliteTodoRepository

位于 `DesktopMemo.Infrastructure` 项目中，使用 SQLite 存储。

**特性**
- 轻量级 SQLite 存储
- 支持排序和筛选
- 自动版本控制

**数据存储位置**
- 数据库：`.memodata/todos.db`

---

## 最佳实践

### 1. 使用工厂方法创建实体

```csharp
// ✅ 正确 - 使用工厂方法
var memo = Memo.CreateNew("标题", "内容");
var todo = TodoItem.CreateNew("待办内容");

// ❌ 错误 - 手动构造（容易遗漏字段）
var memo = new Memo(Guid.NewGuid(), "标题", "内容", ...);
```

### 2. 使用 with 表达式更新

```csharp
// ✅ 正确 - 使用 with 表达式
var updated = existingMemo.WithContent("新内容", DateTimeOffset.Now);

// ❌ 错误 - 不要手动构造更新对象
var updated = new Memo(existingMemo.Id, existingMemo.Title, "新内容", ...);
```

### 3. 错误处理

```csharp
try
{
    await _memoRepository.AddAsync(memo);
}
catch (InvalidOperationException ex)
{
    // 处理备忘录已存在
    Debug.WriteLine($"备忘录已存在: {ex.Message}");
}
catch (IOException ex)
{
    // 处理文件系统错误
    Debug.WriteLine($"文件操作失败: {ex.Message}");
}
```

### 4. 使用取消令牌

```csharp
public async Task LoadMemosAsync(CancellationToken cancellationToken)
{
    var memos = await _memoRepository.GetAllAsync(cancellationToken);
    // 处理数据
}
```

---

## 性能考虑

### 1. 批量操作

目前接口不支持批量操作，如需批量添加或更新，需要逐个调用。未来版本可能会添加批量 API。

```csharp
// 当前实现
foreach (var memo in memos)
{
    await _memoRepository.AddAsync(memo);
}
```

### 2. 查询优化

- `GetAllAsync` 返回所有备忘录，大数据量时可能影响性能
- 考虑添加分页或筛选功能

### 3. 并发控制

- 使用版本号进行乐观并发控制
- 云同步时检测冲突

---

## 相关文档

- [领域模型文档](./Models.md)
- [服务接口文档](./Services.md)
- [数据流和通信](../project_structure/04_数据流和通信.md)

---

**最后更新**：2025-11-15
