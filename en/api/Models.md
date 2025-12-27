# Domain Models

Domain models define the application's core data structures. DesktopMemo uses C# records to keep models immutable and thread-safe.

---

## Memo

The Memo domain model represents a full memo item (Markdown content + metadata).

### Definition

```csharp
namespace DesktopMemo.Core.Models;

/// <summary>
/// Represents a full memo item.
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

### Properties

| Property | Type | Description |
|---|---|---|
| **Id** | `Guid` | Unique identifier |
| **Title** | `string` | Title (reserved field; currently unused) |
| **Content** | `string` | Markdown content |
| **Preview** | `string` | Preview text (first line or first 120 characters) |
| **CreatedAt** | `DateTimeOffset` | Creation time |
| **UpdatedAt** | `DateTimeOffset` | Last update time |
| **Tags** | `IReadOnlyList<string>` | Tags (reserved field for future features) |
| **IsPinned** | `bool` | Whether the memo is pinned |
| **Version** | `int` | Version (for optimistic concurrency) |
| **SyncStatus** | `SyncStatus` | Sync status (reserved for future cloud sync) |
| **DeletedAt** | `DateTimeOffset?` | Deletion time (soft delete marker) |

### Computed Properties

#### DisplayTitle

Returns the display title.

```csharp
public string DisplayTitle => GetDisplayTitle();
```

Logic:
1. If content is empty, use `Title`
2. Otherwise use the first line of the content
3. If the first line is longer than 50 characters, truncate and append `...`

Example:
```csharp
var memo = Memo.CreateNew("", "# Project Plan\n\nStep 1...");
Console.WriteLine(memo.DisplayTitle); // Output: # Project Plan

var memo2 = Memo.CreateNew("", "This is a very long title that exceeds fifty characters and will be truncated");
Console.WriteLine(memo2.DisplayTitle); // Output: This is a very long title that exceeds fifty ch...
```

---

### Factory Methods

#### CreateNew

Creates a new memo.

Signature:
```csharp
public static Memo CreateNew(string title, string content)
```

Parameters:
- `title` - title (usually an empty string)
- `content` - content

Returns:
- a newly created `Memo`

Example:
```csharp
var memo = Memo.CreateNew("", "My first memo");
```

Behavior:
- Generates a new GUID
- Sets `CreatedAt` and `UpdatedAt` to now
- Generates `Preview`
- Initializes as not pinned and not deleted
- Initializes `Version` to 1
- Initializes `SyncStatus` to `Synced`

---

### Update Methods

Because `Memo` is an immutable record, all updates return a new instance.

#### WithContent

Updates the memo content.

Signature:
```csharp
public Memo WithContent(string content, DateTimeOffset timestamp)
```

Parameters:
- `content` - new content
- `timestamp` - update timestamp

Returns:
- a new memo instance with updated content

Example:
```csharp
var updated = memo.WithContent("Updated content", DateTimeOffset.Now);
```

Automatic changes:
- Updates `Content`
- Regenerates `Preview`
- Updates `UpdatedAt`
- Increments `Version` by 1
- Sets `SyncStatus` to `PendingSync`

---

#### WithMetadata

Updates metadata (title, tags, pinned state).

Signature:
```csharp
public Memo WithMetadata(string title, IReadOnlyList<string> tags, bool isPinned, DateTimeOffset timestamp)
```

Parameters:
- `title` - new title
- `tags` - new tags list
- `isPinned` - whether pinned
- `timestamp` - update timestamp

Returns:
- a new memo instance with updated metadata

Example:
```csharp
var tags = new List<string> { "Work", "Important" };
var updated = memo.WithMetadata("New title", tags, isPinned: true, DateTimeOffset.Now);
```

Note: the current version mainly uses `IsPinned`; title and tags are reserved for future features.

---

### Sync-Related Methods

These methods are reserved for future cloud sync.

#### MarkAsSynced

Marks the memo as synced.

Signature:
```csharp
public Memo MarkAsSynced(int remoteVersion)
```

Parameters:
- `remoteVersion` - remote server version

Returns:
- a synced memo instance

Example:
```csharp
var synced = memo.MarkAsSynced(remoteVersion: 5);
```

---

#### MarkAsConflict

Marks the memo as in conflict.

Signature:
```csharp
public Memo MarkAsConflict()
```

Example:
```csharp
var conflicted = memo.MarkAsConflict();
```

---

### Preview Generation

`Preview` is generated automatically from the content:
1. If content is empty, return empty string
2. If content length ≤ 120, return the full content
3. If the first line length < 120, return the first line
4. Otherwise return the first 120 characters + `...`

Example:
```csharp
// Short content
var memo1 = Memo.CreateNew("", "Short");
Console.WriteLine(memo1.Preview); // Output: Short

// Multi-line content
var memo2 = Memo.CreateNew("", "Line 1\nLine 2\nLine 3");
Console.WriteLine(memo2.Preview); // Output: Line 1

// Long content
var longContent = new string('A', 150);
var memo3 = Memo.CreateNew("", longContent);
Console.WriteLine(memo3.Preview.Length); // Output: 123 (120 + "...")
```

---

## TodoItem

The TodoItem domain model represents a single todo item.

### Definition

```csharp
namespace DesktopMemo.Core.Models;

/// <summary>
/// Represents a todo item.
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

### Properties

| Property | Type | Description |
|---|---|---|
| **Id** | `Guid` | Unique identifier |
| **Content** | `string` | Todo text |
| **IsCompleted** | `bool` | Completion state |
| **CreatedAt** | `DateTimeOffset` | Creation time |
| **UpdatedAt** | `DateTimeOffset` | Last update time |
| **SortOrder** | `int` | Sort order (smaller comes first) |
| **Version** | `int` | Version |
| **SyncStatus** | `SyncStatus` | Sync status |
| **CompletedAt** | `DateTimeOffset?` | Completion time |
| **DeletedAt** | `DateTimeOffset?` | Deletion time |

---

### Factory Methods

#### CreateNew

Creates a new todo item.

Signature:
```csharp
public static TodoItem CreateNew(string content, int sortOrder = 0)
```

Parameters:
- `content` - todo text
- `sortOrder` - sort order (default: 0)

Example:
```csharp
var todo = TodoItem.CreateNew("Finish project docs", sortOrder: 0);
```

Behavior:
- Generates a new GUID
- Initializes as not completed
- Sets timestamps automatically
- Sets `SyncStatus` to `PendingSync`

---

### Update Methods

#### ToggleCompleted

Toggles completion state.

Signature:
```csharp
public TodoItem ToggleCompleted()
```

Returns:
- a new todo item instance with toggled state

Example:
```csharp
var completed = todo.ToggleCompleted();   // incomplete -> complete
var uncompleted = completed.ToggleCompleted(); // complete -> incomplete
```

Automatic changes:
- Toggles `IsCompleted`
- Sets `CompletedAt` to now when marking completed
- Clears `CompletedAt` when marking uncompleted
- Updates `UpdatedAt`
- Increments `Version` by 1
- Sets `SyncStatus` to `PendingSync`

---

#### WithContent

Updates the todo content.

Signature:
```csharp
public TodoItem WithContent(string content)
```

Example:
```csharp
var updated = todo.WithContent("Updated todo");
```

---

#### WithSortOrder

Updates the sort order.

Signature:
```csharp
public TodoItem WithSortOrder(int sortOrder)
```

Example:
```csharp
var reordered = todo.WithSortOrder(5);
```

---

#### MarkAsSynced / MarkAsConflict

Sync-related methods; usage is the same as `Memo`.

---

## WindowSettings

The WindowSettings domain model represents window/UI settings.

### Definition

```csharp
namespace DesktopMemo.Core.Models;

/// <summary>
/// Window-related user settings.
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

### Properties

| Property | Type | Default | Description |
|---|---|---:|---|
| **Width** | `double` | 900 | Window width |
| **Height** | `double` | 600 | Window height |
| **Left** | `double` | `NaN` | Window left position |
| **Top** | `double` | `NaN` | Window top position |
| **Transparency** | `double` | 0.05 | Transparency (0.05–0.4) |
| **IsTopMost** | `bool` | false | Always on top |
| **IsDesktopMode** | `bool` | true | Desktop topmost mode |
| **IsClickThrough** | `bool` | false | Click-through mode |
| **ShowDeleteConfirmation** | `bool` | true | Show confirmation when deleting |
| **ShowExitConfirmation** | `bool` | true | Show confirmation when exiting |
| **DefaultExitToTray** | `bool` | true | Exit to tray by default |
| **PreferredLanguage** | `string` | "zh-CN" | Preferred language |
| **CurrentPage** | `string` | "memo" | Current page (memo/todo) |
| **TodoInputVisible** | `bool` | true | Whether the todo input is visible |
| **Theme** | `AppTheme` | `System` | App theme |
| **IsWindowPinned** | `bool` | false | Whether the window is pinned |

---

### Default Settings

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

Notes:
- `Left` and `Top` being `NaN` means the window is centered
- Desktop topmost mode is enabled by default
- Default transparency is 5%
- Default theme follows the system

---

### Update Methods

#### WithLocation

Updates window position.

Signature:
```csharp
public WindowSettings WithLocation(double left, double top)
```

Example:
```csharp
var newSettings = settings.WithLocation(100, 200);
```

---

#### WithSize

Updates window size.

Signature:
```csharp
public WindowSettings WithSize(double width, double height)
```

Example:
```csharp
var newSettings = settings.WithSize(1200, 800);
```

---

#### WithAppearance

Updates appearance settings.

Signature:
```csharp
public WindowSettings WithAppearance(double transparency, bool topMost, bool desktopMode, bool clickThrough)
```

Example:
```csharp
var newSettings = settings.WithAppearance(
    transparency: 0.2,
    topMost: true,
    desktopMode: false,
    clickThrough: false
);
```

---

### Using `with`

Because WindowSettings is an immutable record, you can also update it using `with`:

```csharp
// Update a single property
var newSettings = settings with { Theme = AppTheme.Dark };

// Update multiple properties
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

Log entry model (simplified).

### Definition

```csharp
namespace DesktopMemo.Core.Models;

public sealed record LogEntry(
    DateTimeOffset Timestamp,
    LogLevel Level,
    string Message,
    string? Exception = null)
```

### Properties

| Property | Type | Description |
|---|---|---|
| **Timestamp** | `DateTimeOffset` | Timestamp |
| **Level** | `LogLevel` | Log level |
| **Message** | `string` | Message |
| **Exception** | `string?` | Exception info (optional) |

---

## Best Practices

### 1. Use factory methods

```csharp
// ✅ Correct - use factory methods
var memo = Memo.CreateNew("", "Content");
var todo = TodoItem.CreateNew("Todo");

// ❌ Wrong - manual construction is error-prone
var memo = new Memo(Guid.NewGuid(), "", "Content", ...);
```

---

### 2. Use `with` or dedicated update methods

```csharp
// ✅ Correct
var updated = memo.WithContent("New content", DateTimeOffset.Now);
var updated2 = settings with { Theme = AppTheme.Dark };

// ❌ Wrong - records are immutable
memo.Content = "New content"; // compile error
```

---

### 3. Check nullable fields

```csharp
if (memo.DeletedAt.HasValue)
{
    Console.WriteLine($"Deleted at {memo.DeletedAt.Value}");
}

if (todo.CompletedAt.HasValue)
{
    var duration = todo.CompletedAt.Value - todo.CreatedAt;
    Console.WriteLine($"Duration: {duration.TotalHours} hours");
}
```

---

### 4. Versioning and conflict detection

```csharp
public async Task UpdateMemoAsync(Memo updatedMemo)
{
    var current = await _repository.GetByIdAsync(updatedMemo.Id);

    if (current == null)
    {
        throw new InvalidOperationException("Memo not found");
    }

    if (current.Version != updatedMemo.Version)
    {
        throw new InvalidOperationException("Version conflict: data was modified by another operation");
    }

    await _repository.UpdateAsync(updatedMemo);
}
```

---

### 5. Prefer read-only collections

```csharp
// ✅ Correct
var tags = new List<string> { "Work", "Important" }.AsReadOnly();
var memo = memo.WithMetadata("Title", tags, true, DateTimeOffset.Now);

// ❌ Wrong - passing a mutable list can cause unexpected changes
var tags = new List<string> { "Work", "Important" };
var memo = memo.WithMetadata("Title", tags, true, DateTimeOffset.Now);
tags.Add("Urgent"); // this can affect the memo
```

---

## Benefits of Immutability

### 1. Thread safety

```csharp
var memo = Memo.CreateNew("", "Content");

Task.Run(() => Console.WriteLine(memo.Content)); // thread 1
Task.Run(() => Console.WriteLine(memo.Content)); // thread 2
```

---

### 2. History tracking

```csharp
var history = new List<Memo>();
var current = Memo.CreateNew("", "Initial");
history.Add(current);

current = current.WithContent("Version 2", DateTimeOffset.Now);
history.Add(current);

current = current.WithContent("Version 3", DateTimeOffset.Now);
history.Add(current);

Console.WriteLine(history[0].Content); // Initial
Console.WriteLine(history[1].Content); // Version 2
```

---

### 3. Simpler state management

```csharp
var darkSettings = settings with { Theme = AppTheme.Dark };

Console.WriteLine(settings.Theme);     // Light
Console.WriteLine(darkSettings.Theme); // Dark
```

---

## Related Docs

- [Enum Types](./Enums.md)
- [Repository Interfaces](./Repositories.md)
- [Service Interfaces](./Services.md)

---

**Last Updated**: 2025-11-15
