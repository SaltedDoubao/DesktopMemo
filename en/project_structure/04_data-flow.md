# Data Flow and Communication

## 1. Overview

```mermaid
graph TB
    subgraph "User Interaction"
        User[User actions]
        UI[WPF UI Views]
    end

    subgraph "App Logic"
        VM[ViewModels]
        Commands[Commands]
    end

    subgraph "Business Services"
        Repos[Repositories]
        Services[Services]
    end

    subgraph "Persistence"
        FS[File System]
        DB[(SQLite)]
        Settings[JSON Files]
    end

    User -->|click/type| UI
    UI -->|data binding| VM
    UI -->|trigger command| Commands
    Commands -->|call method| VM
    VM -->|read/write| Repos
    VM -->|business logic| Services
    Repos -->|persist| FS
    Repos -->|index/query| DB
    Services -->|config| Settings

    DB -.notify.-> Repos
    Repos -.update.-> VM
    VM -.PropertyChanged.-> UI
    UI -.render.-> User

    style User fill:#fff9c4
    style UI fill:#e3f2fd
    style VM fill:#f3e5f5
    style Repos fill:#e8f5e9
    style DB fill:#c8e6c9
```

---

## 2. Core Data Flow Scenarios

### 2.1 Application Startup

```mermaid
sequenceDiagram
    participant User
    participant App as App.xaml.cs
    participant DI as DI Container
    participant Migration as Migration Services
    participant VM as MainViewModel
    participant Repo as Repositories
    participant UI as MainWindow

    User->>App: Launch
    App->>DI: Configure services (ConfigureServices)
    DI->>DI: Register Repositories
    DI->>DI: Register Services
    DI->>DI: Register ViewModels

    App->>Migration: Execute migrations
    Migration->>Migration: TodoMigrationService (JSON→SQLite)
    Migration->>Migration: MemoMetadataMigrationService
    Migration-->>App: Done

    App->>DI: Resolve MainViewModel
    DI->>VM: Create instance (inject dependencies)
    App->>VM: InitializeAsync()

    VM->>Repo: GetAllAsync() - load memos
    Repo-->>VM: List<Memo>
    VM->>VM: Populate Memos collection

    App->>UI: Create MainWindow
    UI->>UI: Bind to ViewModel
    App->>UI: window.Show()
    UI-->>User: UI displayed
```

Key steps:
1. DI configuration (`App.xaml.cs:23-54`)
2. Migration check (`App.xaml.cs:73-101`)
3. ViewModel initialization (`MainViewModel.InitializeAsync`)
4. Load memo list (via Repository)
5. Show window

Data sources:
- Memos: `.memodata/memos/*.md` + `memos.db`
- Settings: `.memodata/settings.json`
- TodoList: `todos.db`

---

### 2.2 Create a New Memo

```mermaid
sequenceDiagram
    participant User
    participant UI as MainWindow
    participant VM as MainViewModel
    participant Repo as SqliteIndexedMemoRepository
    participant FS as File System
    participant DB as SQLite Database

    User->>UI: Click "New Memo"
    UI->>VM: CreateMemoCommand.Execute()

    VM->>VM: Memo.CreateNew(title, content)
    VM->>Repo: AddAsync(newMemo)

    Repo->>FS: Write Markdown file
    Note over FS: .memodata/memos/{id}.md
    FS-->>Repo: OK

    Repo->>DB: INSERT INTO memos
    DB-->>Repo: OK

    Repo-->>VM: OK
    VM->>VM: Memos.Insert(0, newMemo)
    VM->>VM: SelectedMemo = newMemo
    VM->>VM: IsEditMode = true

    VM-->>UI: PropertyChanged
    UI->>UI: Refresh list
    UI->>UI: Focus editor
    UI-->>User: New memo shown
```

Data flow:
1. UI → ViewModel: command triggered
2. ViewModel → Repository: `AddAsync`
3. Repository → File System: write Markdown file
4. Repository → SQLite: insert metadata index
5. Repository → ViewModel: returns OK
6. ViewModel → UI: property notifications (`INotifyPropertyChanged`)

Files involved:
- `MainViewModel.cs:CreateMemoCommand`
- `SqliteIndexedMemoRepository.cs:AddAsync`
- `.memodata/memos/{guid}.md`
- `.memodata/memos.db`

---

### 2.3 Edit Memo

```mermaid
sequenceDiagram
    participant User
    participant Editor as TextBox (Editor)
    participant VM as MainViewModel
    participant Debouncer as DebounceHelper
    participant Repo as SqliteIndexedMemoRepository
    participant FS as File System
    participant DB as SQLite Database

    User->>Editor: Type
    Editor->>VM: EditorContent changed

    VM->>VM: OnEditorContentChanged
    VM->>Debouncer: Debounce(500ms, SaveCurrentMemoAsync)
    Note over Debouncer: delay 500ms

    Debouncer->>VM: Run SaveCurrentMemoAsync
    VM->>VM: SelectedMemo.WithContent(content, timestamp)
    VM->>Repo: UpdateAsync(updatedMemo)

    Repo->>FS: Update Markdown file
    FS-->>Repo: OK

    Repo->>DB: UPDATE memos SET updated_at = ?
    DB-->>Repo: OK

    Repo-->>VM: OK
    VM->>VM: Update Memos collection
    VM-->>Editor: PropertyChanged
    Editor-->>User: UI stays responsive
```

Debounce strategy:
- Delay: 500ms
- Goal: avoid frequent saves (performance)
- Implementation: `Core/Helpers/DebounceHelper.cs`

Synchronization:
- Editor text (`EditorContent`) ← user input
- In-memory model (`SelectedMemo`)
- Markdown file (persistence)
- SQLite index (metadata)

---

### 2.4 Search Memos

```mermaid
sequenceDiagram
    participant User
    participant SearchBox as SearchBox
    participant VM as MainViewModel
    participant SearchService as MemoSearchService
    participant Repo as SqliteIndexedMemoRepository
    participant DB as SQLite Database

    User->>SearchBox: Type keyword
    SearchBox->>VM: SearchKeyword changed

    VM->>VM: OnSearchKeywordChanged
    VM->>SearchService: SearchAsync(keyword)

    SearchService->>Repo: GetAllAsync()
    Repo->>DB: SELECT * FROM memos
    DB-->>Repo: All memos
    Repo-->>SearchService: List<Memo>

    SearchService->>SearchService: In-memory filter (LINQ)
    Note over SearchService: Match Title/Content
    SearchService-->>VM: Results

    VM->>VM: Memos = searchResults
    VM-->>SearchBox: PropertyChanged
    SearchBox-->>User: Show results
```

Search strategy:
1. Load all memos from Repository
2. Filter in memory with LINQ
3. Match rules:
   - Title contains keyword
   - Content contains keyword
   - Tags contains keyword

Performance ideas:
- Use SQLite FTS5
- Load only metadata (Title, Preview)
- Lazy-load full content

---

### 2.5 Save Settings

```mermaid
sequenceDiagram
    participant User
    participant Slider as Slider (Transparency)
    participant VM as MainViewModel
    participant Debouncer as DebounceHelper
    participant SettingsService as JsonSettingsService
    participant JSON as settings.json

    User->>Slider: Drag
    Slider->>VM: BackgroundOpacity changed

    VM->>VM: OnBackgroundOpacityChanged
    VM->>Debouncer: Debounce(500ms, SaveSettingsAsync)
    Note over Debouncer: debounce 500ms

    Debouncer->>VM: Run SaveSettingsAsync
    VM->>VM: Collect all settings (WindowSettings)
    VM->>SettingsService: SaveAsync(windowSettings)

    SettingsService->>JSON: Serialize as JSON
    SettingsService->>JSON: Write file
    JSON-->>SettingsService: OK

    SettingsService-->>VM: OK
    VM-->>Slider: UI stays responsive
```

Setting types:
- Window: position, size, transparency
- App: theme, language, auto start
- Topmost mode: desktop / always / normal

Why debounce:
- Slider drag triggers frequent property changes
- Avoid writing file for every tick (I/O is expensive)
- Save after 500ms idle

Atomic save:
```csharp
var tempPath = settingsPath + ".tmp";
await File.WriteAllTextAsync(tempPath, json);
File.Move(tempPath, settingsPath, overwrite: true);
```

---

### 2.6 TodoList Data Flow

```mermaid
sequenceDiagram
    participant User
    participant UI as TodoList View
    participant TodoVM as TodoListViewModel
    participant TodoRepo as SqliteTodoRepository
    participant DB as todos.db

    User->>UI: Click "Add"
    UI->>TodoVM: AddTodoCommand.Execute()

    TodoVM->>TodoVM: Create TodoItem
    TodoVM->>TodoRepo: AddAsync(todoItem)

    TodoRepo->>DB: INSERT INTO todos
    DB-->>TodoRepo: OK

    TodoRepo-->>TodoVM: OK
    TodoVM->>TodoVM: TodoItems.Add(todoItem)
    TodoVM-->>UI: PropertyChanged
    UI-->>User: New item shown

    User->>UI: Toggle checkbox
    UI->>TodoVM: ToggleTodoCommand.Execute()

    TodoVM->>TodoVM: Toggle IsCompleted
    TodoVM->>TodoRepo: UpdateAsync(todoItem)

    TodoRepo->>DB: UPDATE todos SET is_completed = 1
    DB-->>TodoRepo: OK

    TodoRepo-->>TodoVM: OK
    TodoVM->>TodoVM: Move to completed list
    TodoVM-->>UI: PropertyChanged
    UI-->>User: UI updated
```

Characteristics:
- SQLite-only storage (no files)
- Transaction support
- Two-list management:
  - `TodoItems`
  - `CompletedTodoItems`

---

## 3. Storage Strategy

### 3.1 Dual Storage Design for Memos

```mermaid
graph LR
    subgraph "Memory"
        Memo[Memo object]
    end

    subgraph "Persistence"
        MD[Markdown file<br/>full content]
        DB[SQLite index<br/>metadata]
    end

    Memo -->|serialize| MD
    Memo -->|extract metadata| DB

    MD -.load.-> Memo
    DB -.fast query.-> Memo

    style Memo fill:#f3e5f5
    style MD fill:#ffe0b2
    style DB fill:#c8e6c9
```

Why dual storage?

1. Markdown files (source of truth)
   - ✅ Human-readable
   - ✅ Version-control friendly
   - ✅ Editable with external editors
   - ✅ Data safety (even if SQLite breaks)
   - ❌ Slow to query (file traversal)

2. SQLite index (performance)
   - ✅ Fast queries (SQL)
   - ✅ Sorting & filtering
   - ✅ Full-text search (FTS5)
   - ❌ Binary format (not directly editable)

Sync strategy:
```csharp
await File.WriteAllTextAsync(filePath, markdown); // write file first
await _db.ExecuteAsync("INSERT INTO memos...");   // then update index

var metadata = await _db.QueryAsync<Memo>("SELECT * FROM memos"); // read index
var content = await File.ReadAllTextAsync(filePath);              // read full content
```

---

### 3.2 Data Consistency Guarantees

Problem: what if the Markdown file write succeeds, but SQLite insert fails?

Solutions:

#### Option 1: Dual-write + rollback
```csharp
await File.WriteAllTextAsync(filePath, markdown);
try
{
    await _db.ExecuteAsync("INSERT INTO memos...");
}
catch
{
    File.Delete(filePath); // rollback file write
    throw;
}
```

#### Option 2: Startup repair
```csharp
var filesOnDisk = Directory.GetFiles(".memodata/memos", "*.md");
var idsInDb = await _db.QueryAsync<string>("SELECT id FROM memos");

var missingInDb = filesOnDisk.Except(idsInDb);
foreach (var id in missingInDb)
{
    var memo = await ParseMarkdownFile(id);
    await _db.ExecuteAsync("INSERT INTO memos...");
}
```

Current implementation: Option 1 (dual-write + exception handling).

---

### 3.3 Concurrency Control

Problem: what if multiple operations modify the same memo at the same time?

Current strategy:
- Single-threaded UI: all UI operations are on the WPF UI thread
- No user-level concurrency: user edits one memo at a time

Future (cloud sync):
- Use `Version` for optimistic concurrency
- Conflict check: `WHERE id = ? AND version = ?`
- Mark conflicts as `SyncStatus.Conflict`

---

## 4. Async Data Flow

### 4.1 Async programming pattern

Rule: avoid `async void` and use fire-and-forget patterns where appropriate.

Bad example:
```csharp
private async void Button_Click(object sender, RoutedEventArgs e)
{
    await SaveMemoAsync(); // ❌ hard to observe errors; can lead to subtle issues
}
```

Good example:
```csharp
private void Button_Click(object sender, RoutedEventArgs e)
{
    _ = Task.Run(async () => await SaveMemoAsync()); // ✅ fire-and-forget
}
```

Async commands in ViewModels:
```csharp
[RelayCommand]
private async Task SaveMemoAsync() // ✅ return Task
{
    await _repository.UpdateAsync(SelectedMemo);
}
```

---

### 4.2 Thread switching

```mermaid
graph LR
    UIThread[UI thread] -->|Task.Run| BgThread[Background thread]
    BgThread -->|File I/O| FS[File System]
    BgThread -->|SQLite| DB[Database]
    BgThread -->|Dispatcher.Invoke| UIThread
    UIThread -->|PropertyChanged| UI[UI update]

    style UIThread fill:#e3f2fd
    style BgThread fill:#fff3e0
    style UI fill:#c8e6c9
```

Example:
```csharp
private async Task LoadMemosAsync()
{
    var memos = await Task.Run(async () =>
        await _repository.GetAllAsync());

    Memos = new ObservableCollection<Memo>(memos);
}
```

---

## 5. Event-Driven Data Flow

### 5.1 Property change notifications

```mermaid
graph LR
    VM[ViewModel<br/>ObservableObject] -->|PropertyChanged| Binding[Data Binding]
    Binding -->|update| UI[UI Element]
    UI -->|user input| Binding
    Binding -->|set property| VM

    style VM fill:#f3e5f5
    style UI fill:#e3f2fd
```

Implementation:
```csharp
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _editorContent = string.Empty;

    // generated:
    public string EditorContent
    {
        get => _editorContent;
        set
        {
            if (_editorContent != value)
            {
                _editorContent = value;
                OnPropertyChanged(nameof(EditorContent));
            }
        }
    }
}
```

XAML binding:
```xml
<TextBox Text="{Binding EditorContent, UpdateSourceTrigger=PropertyChanged}" />
```

---

### 5.2 Collection change notifications

`ObservableCollection`:
```csharp
public ObservableCollection<Memo> Memos { get; } = new();

Memos.Add(newMemo);
Memos.Remove(oldMemo);
Memos[0] = updatedMemo;
```

Notes:
- Implements `INotifyCollectionChanged`
- WPF binding listens to `CollectionChanged` and updates UI automatically

---

## 6. Key Design Patterns in the Data Flow

### 6.1 Repository pattern

Purpose: encapsulate data access.

```csharp
// contract (Core)
public interface IMemoRepository
{
    Task<IReadOnlyList<Memo>> GetAllAsync();
    Task AddAsync(Memo memo);
}

// implementation (Infrastructure)
public class SqliteIndexedMemoRepository : IMemoRepository
{
    public async Task<IReadOnlyList<Memo>> GetAllAsync()
    {
        // load from SQLite + Markdown
    }
}

// usage (App)
public class MainViewModel
{
    private readonly IMemoRepository _repository;

    public MainViewModel(IMemoRepository repository)
    {
        _repository = repository;
    }
}
```

---

### 6.2 Unidirectional data flow

```mermaid
graph TB
    User[User action] -->|1. trigger| Command[Command]
    Command -->|2. call| VM[ViewModel method]
    VM -->|3. update| Model[Model]
    Model -->|4. persist| Storage[Storage layer]
    Storage -.5. return.-> VM
    VM -.6. PropertyChanged.-> UI[UI update]
    UI -.7. render.-> User

    style User fill:#fff9c4
    style Command fill:#f3e5f5
    style VM fill:#e1bee7
    style Storage fill:#c8e6c9
    style UI fill:#e3f2fd
```

Benefits:
- Clear flow
- Easier debugging and testing
- Avoid complexity of two-way state mutations

---

### 6.3 Debounce pattern

Purpose: reduce execution frequency for high-frequency events.

```csharp
// DebounceHelper.cs
public class DebounceHelper
{
    private CancellationTokenSource? _cts;

    public void Debounce(int delayMs, Action action)
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        Task.Delay(delayMs, _cts.Token)
            .ContinueWith(_ => action(),
                TaskScheduler.FromCurrentSynchronizationContext());
    }
}
```

Use cases:
- Editor autosave (500ms)
- Search box input (300ms)
- Settings sliders (500ms)

---

## 7. Exception Handling in the Data Flow

### 7.1 Layered exception handling

```mermaid
graph TB
    UI[UI layer] -->|catch| UIEx[Show friendly message]
    VM[ViewModel layer] -->|catch| VMEx[Log + retry]
    Repo[Repository layer] -->|catch| RepoEx[Wrap + throw]
    Storage[Storage] -->|throw| RawEx[Raw exception]

    RawEx -.bubble.-> RepoEx
    RepoEx -.bubble.-> VMEx
    VMEx -.bubble.-> UIEx
```

Example:
```csharp
// Repository
public async Task AddAsync(Memo memo)
{
    try
    {
        await File.WriteAllTextAsync(path, content);
    }
    catch (IOException ex)
    {
        throw new DataAccessException("Cannot write memo file", ex);
    }
}

// ViewModel
[RelayCommand]
private async Task SaveMemoAsync()
{
    try
    {
        await _repository.UpdateAsync(SelectedMemo);
    }
    catch (DataAccessException ex)
    {
        _logService.Error("MainViewModel", "Save failed", ex);
        // notify user
    }
}
```

---

## 8. Data Flow Performance Optimization

### 8.1 Lazy loading

Strategy: load full content only when needed.

```csharp
var memos = await _db.QueryAsync<Memo>("SELECT id, title, preview FROM memos");
var content = await File.ReadAllTextAsync($".memodata/memos/{id}.md");
```

---

### 8.2 List virtualization (not implemented)

Suggestion: when memo count exceeds ~1000.
```xml
<ListBox VirtualizingPanel.IsVirtualizing="True"
         VirtualizingPanel.VirtualizationMode="Recycling" />
```

---

### 8.3 Caching

Current: in-memory cache (`ObservableCollection`).
```csharp
public ObservableCollection<Memo> Memos { get; } = new();
```

Future optimization: LRU cache
- Cache only the most recently used 100 items
- Load the rest on demand

---

## 9. Data Flow Security

### 9.1 SQL injection prevention

Use parameterized queries:
```csharp
await _db.ExecuteAsync(
    "INSERT INTO memos (id, title) VALUES (@Id, @Title)",
    new { Id = memo.Id, Title = memo.Title });
```

Avoid string interpolation:
```csharp
await _db.ExecuteAsync(
    $"INSERT INTO memos (id, title) VALUES ('{memo.Id}', '{memo.Title}')");
```

---

### 9.2 File path safety

Validate file names:
```csharp
var fileName = $"{memo.Id}.md";
if (fileName.Contains("..") || fileName.Contains("/") || fileName.Contains("\\"))
{
    throw new SecurityException("Invalid file name");
}
var fullPath = Path.Combine(_dataDirectory, "memos", fileName);
```

---

## 10. Monitoring and Debugging

### 10.1 Logging

Key nodes:
```csharp
_logService.Debug("MainViewModel", $"Start loading memos");
var memos = await _repository.GetAllAsync();
_logService.Info("MainViewModel", $"Loaded {memos.Count} memos");
```

Log location: `.memodata/logs/app-{date}.log`

---

### 10.2 Debug output

```csharp
System.Diagnostics.Debug.WriteLine($"[MainViewModel] Saving memo: {memo.Id}");
```

View in: Visual Studio Output window.

---

## 11. Summary

### Core data flow path

1. User input → UI (WPF)
2. UI → ViewModel (binding/commands)
3. ViewModel → Repository/Service
4. Repository → File system / SQLite
5. Persistence OK → ViewModel
6. ViewModel → UI (property notifications)
7. UI → user (render)

### Design principles

- ✅ Unidirectional data flow
- ✅ Layered architecture
- ✅ Dependency inversion (interfaces in Core)
- ✅ Async-first (avoid blocking UI)
- ✅ Debounce optimizations
- ✅ Layered exception handling
- ✅ Logging at key nodes

---

**Last Updated**: 2025-11-15
