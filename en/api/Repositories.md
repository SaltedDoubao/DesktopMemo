# Repository Interfaces

Repository interfaces provide the core contracts for data persistence access. They follow the Repository pattern and isolate data access logic.

---

## IMemoRepository

Memo data access interface (CRUD).

### Interface Definition

```csharp
namespace DesktopMemo.Core.Contracts;

/// <summary>
/// Contract for memo persistence access.
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

### Method Details

#### GetAllAsync

Returns all memos.

Signature:
```csharp
Task<IReadOnlyList<Memo>> GetAllAsync(CancellationToken cancellationToken = default)
```

Parameters:
- `cancellationToken` - cancels the async operation

Returns:
- `Task<IReadOnlyList<Memo>>` - a read-only list containing all memos

Example:
```csharp
var memos = await _memoRepository.GetAllAsync();
foreach (var memo in memos)
{
    Console.WriteLine($"{memo.Title}: {memo.Preview}");
}
```

Notes:
- The returned list is read-only
- Memos are ordered by update time descending (newest first)
- Pinned memos are placed first

---

#### GetByIdAsync

Gets a memo by ID.

Signature:
```csharp
Task<Memo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
```

Parameters:
- `id` - memo ID
- `cancellationToken` - cancels the async operation

Returns:
- `Task<Memo?>` - the memo if found, otherwise `null`

Example:
```csharp
var memoId = Guid.Parse("...");
var memo = await _memoRepository.GetByIdAsync(memoId);

if (memo != null)
{
    Console.WriteLine($"Found memo: {memo.Title}");
}
else
{
    Console.WriteLine("Memo not found");
}
```

Notes:
- Returns `null` if the ID does not exist (no exception)
- Prefer checking for `null` before using the result

---

#### AddAsync

Adds a new memo.

Signature:
```csharp
Task AddAsync(Memo memo, CancellationToken cancellationToken = default)
```

Parameters:
- `memo` - memo to add
- `cancellationToken` - cancels the async operation

Returns:
- `Task`

Example:
```csharp
var newMemo = Memo.CreateNew("My title", "Memo content");
await _memoRepository.AddAsync(newMemo);
```

Notes:
- Saved to both SQLite and the Markdown file
- Throws if the ID already exists
- Prefer `Memo.CreateNew()` to ensure correct initialization

Possible exceptions:
- `InvalidOperationException` - memo ID already exists
- `IOException` - file system operation failed
- `UnauthorizedAccessException` - no write permission

---

#### UpdateAsync

Updates an existing memo.

Signature:
```csharp
Task UpdateAsync(Memo memo, CancellationToken cancellationToken = default)
```

Parameters:
- `memo` - memo with updated data
- `cancellationToken` - cancels the async operation

Returns:
- `Task`

Example:
```csharp
var existingMemo = await _memoRepository.GetByIdAsync(memoId);
if (existingMemo != null)
{
    var updatedMemo = existingMemo.WithContent(
        "New content",
        DateTimeOffset.Now
    );
    await _memoRepository.UpdateAsync(updatedMemo);
}
```

Notes:
- Prefer `with` / record update methods instead of reconstructing manually
- Updates both the database and the Markdown file
- `Version` is incremented automatically
- `SyncStatus` is set to `PendingSync` automatically

Possible exceptions:
- `InvalidOperationException` - memo does not exist
- `IOException` - file system operation failed
- `UnauthorizedAccessException` - no write permission

---

#### DeleteAsync

Deletes a memo by ID.

Signature:
```csharp
Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
```

Parameters:
- `id` - memo ID
- `cancellationToken` - cancels the async operation

Returns:
- `Task`

Example:
```csharp
await _memoRepository.DeleteAsync(memoId);
```

Notes:
- Deletes both the DB record and the Markdown file
- Soft delete: actually sets `DeletedAt` so data can still exist (reserved for cloud sync)
- If the ID does not exist, the operation is idempotent (no exception)

Possible exceptions:
- `IOException` - file system operation failed
- `UnauthorizedAccessException` - no delete permission

---

## ITodoRepository

Todo data access interface (CRUD).

### Interface Definition

```csharp
namespace DesktopMemo.Core.Contracts;

/// <summary>
/// Contract for todo persistence access.
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

### Method Details

#### GetAllAsync

Returns all todos.

Signature:
```csharp
Task<IReadOnlyList<TodoItem>> GetAllAsync(CancellationToken cancellationToken = default)
```

Parameters:
- `cancellationToken` - cancels the async operation

Returns:
- `Task<IReadOnlyList<TodoItem>>` - a read-only list containing all todos

Example:
```csharp
var todos = await _todoRepository.GetAllAsync();
var pendingTodos = todos.Where(t => !t.IsCompleted).ToList();
var completedTodos = todos.Where(t => t.IsCompleted).ToList();
```

Notes:
- Ordered by `SortOrder`
- Pending todos are placed before completed ones
- Deleted items are not returned

---

#### GetByIdAsync

Gets a todo by ID.

Signature:
```csharp
Task<TodoItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
```

Parameters:
- `id` - todo ID
- `cancellationToken` - cancels the async operation

Returns:
- `Task<TodoItem?>` - the todo if found, otherwise `null`

Example:
```csharp
var todo = await _todoRepository.GetByIdAsync(todoId);
if (todo != null)
{
    Console.WriteLine($"Todo: {todo.Content}, Status: {(todo.IsCompleted ? "Completed" : "Pending")}");
}
```

---

#### AddAsync

Adds a new todo.

Signature:
```csharp
Task AddAsync(TodoItem todoItem, CancellationToken cancellationToken = default)
```

Parameters:
- `todoItem` - todo to add
- `cancellationToken` - cancels the async operation

Returns:
- `Task`

Example:
```csharp
var newTodo = TodoItem.CreateNew("Finish project docs", sortOrder: 0);
await _todoRepository.AddAsync(newTodo);
```

Notes:
- Prefer `TodoItem.CreateNew()` to ensure correct initialization
- Newly created todos are incomplete by default
- `SortOrder` controls ordering (smaller comes first)

---

#### UpdateAsync

Updates an existing todo.

Signature:
```csharp
Task UpdateAsync(TodoItem todoItem, CancellationToken cancellationToken = default)
```

Parameters:
- `todoItem` - todo with updated data
- `cancellationToken` - cancels the async operation

Returns:
- `Task`

Example:
```csharp
var todo = await _todoRepository.GetByIdAsync(todoId);
if (todo != null)
{
    var updatedTodo = todo.ToggleCompleted();
    await _todoRepository.UpdateAsync(updatedTodo);
}
```

Common update patterns:
```csharp
var updated = todo.ToggleCompleted();
var updated = todo.WithContent("New todo content");
var updated = todo.WithSortOrder(5);
```

---

#### DeleteAsync

Deletes a todo by ID.

Signature:
```csharp
Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
```

Parameters:
- `id` - todo ID
- `cancellationToken` - cancels the async operation

Returns:
- `Task`

Example:
```csharp
await _todoRepository.DeleteAsync(todoId);
```

Notes:
- Soft delete: sets `DeletedAt` so the data can still exist
- Idempotent operation: deleting twice does not error

---

## Implementations

### SqliteIndexedMemoRepository

Located in the `DesktopMemo.Infrastructure` project; implements dual storage using SQLite + Markdown.

Features:
- SQLite FTS5 full-text search index
- Markdown files store the complete content
- Transaction support
- Automatic versioning

Storage locations:
- Database: `.memodata/memos.db`
- Files: `.memodata/content/{memoId}.md`

### SqliteTodoRepository

Located in the `DesktopMemo.Infrastructure` project; uses SQLite storage.

Features:
- Lightweight SQLite storage
- Sorting and filtering
- Automatic versioning

Storage locations:
- Database: `.memodata/todos.db`

---

## Best Practices

### 1. Use factory methods

```csharp
var memo = Memo.CreateNew("Title", "Content");
var todo = TodoItem.CreateNew("Todo content");
```

---

### 2. Use `with` updates

```csharp
var updated = existingMemo.WithContent("New content", DateTimeOffset.Now);
```

---

### 3. Error handling

```csharp
try
{
    await _memoRepository.AddAsync(memo);
}
catch (InvalidOperationException ex)
{
    Debug.WriteLine($"Memo already exists: {ex.Message}");
}
catch (IOException ex)
{
    Debug.WriteLine($"File operation failed: {ex.Message}");
}
```

---

### 4. Use cancellation tokens

```csharp
public async Task LoadMemosAsync(CancellationToken cancellationToken)
{
    var memos = await _memoRepository.GetAllAsync(cancellationToken);
}
```

---

## Performance Considerations

### 1. Batch operations

The current interfaces do not support batch operations. If you need to add or update many items, call methods one by one. Future versions may add batch APIs.

```csharp
foreach (var memo in memos)
{
    await _memoRepository.AddAsync(memo);
}
```

### 2. Query optimization

- `GetAllAsync` returns all memos; large datasets may impact performance
- Consider pagination or filtering in the future

### 3. Concurrency control

- Uses `Version` for optimistic concurrency control
- Reserved conflict detection for cloud sync

---

## Related Docs

- [Domain Models](./Models.md)
- [Service Interfaces](./Services.md)
- [Data Flow & Communication](../project_structure/04_data-flow.md)

---

**Last Updated**: 2025-11-15
