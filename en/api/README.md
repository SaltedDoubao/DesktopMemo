# DesktopMemo API Documentation

This directory contains complete API documentation for the DesktopMemo project, detailing the usage of core interfaces, models, and services.

---

## 📚 Documentation Contents

### [1. Repository Interfaces (Repositories)](./Repositories.md)
Core interfaces providing data persistence access:
- **IMemoRepository** - Memo data access
- **ITodoRepository** - Todo item data access

### [2. Service Interfaces (Services)](./Services.md)
Service interfaces providing business logic and system functionality:
- **ISettingsService** - Application settings management
- **IMemoSearchService** - Memo search
- **IWindowService** - Window management (pin, transparency, mouse-through, etc.)
- **ITrayService** - System tray service
- **ILocalizationService** - Multi-language localization service
- **ILogService** - Logging service

### [3. Domain Models (Models)](./Models.md)
Core application data structures:
- **Memo** - Memo model
- **TodoItem** - Todo item model
- **WindowSettings** - Window settings model
- **LogEntry** - Log entry model

### [4. Enum Types (Enums)](./Enums.md)
Enum types used in the application:
- **TopmostMode** - Window pin mode
- **AppTheme** - Application theme
- **SyncStatus** - Sync status
- **LogLevel** - Log level

---

## 🎯 Quick Start

### Dependency Injection Configuration

DesktopMemo uses Microsoft.Extensions.DependencyInjection for dependency injection. Core services are registered in `src/DesktopMemo.App/App.xaml.cs` (some implementations require `dataDirectory` parameter).

```csharp
// App.xaml.cs
private void ConfigureServices(IServiceCollection services)
{
    // Repositories
    services.AddSingleton<IMemoRepository>(_ => new SqliteIndexedMemoRepository(dataDirectory));
    services.AddSingleton<ITodoRepository>(_ => new SqliteTodoRepository(dataDirectory));

    // Services
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

### Usage in ViewModels

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
        // Process memo list
    }
}
```

---

## 🏗️ Architecture Design

### Three-Layer Architecture

```
┌─────────────────────────────────────────┐
│  Presentation Layer (DesktopMemo.App)   │
│  - ViewModels use interfaces            │
│  - No direct dependency on implementations │
├─────────────────────────────────────────┤
│  Domain Layer (DesktopMemo.Core)        │
│  - Defines interfaces (Contracts)       │
│  - Defines models (Models)              │
│  - Pure .NET library, no external deps  │
├─────────────────────────────────────────┤
│  Infrastructure (DesktopMemo.Infrastructure) │
│  - Implements interfaces                │
│  - Handles data access and external system interaction │
└─────────────────────────────────────────┘
```

### Dependency Direction

- **Presentation Layer** → **Domain Layer** (depends on interfaces)
- **Infrastructure Layer** → **Domain Layer** (implements interfaces)
- **Domain Layer** depends on no other layers (core)

---

## 🔄 Data Flow

### Memo CRUD Flow

```
User Action (View)
    ↓
Command Triggered (ViewModel)
    ↓
Call Interface Method (IMemoRepository)
    ↓
Implementation Handles (SqliteIndexedMemoRepository)
    ↓
Data Persistence (SQLite + Markdown)
    ↓
Update ViewModel State
    ↓
UI Update (Data Binding)
```

### Settings Save Flow

```
User Modifies Settings
    ↓
ViewModel Updates State
    ↓
Call ISettingsService.SaveAsync()
    ↓
Async Save to JSON File
    ↓
Update Memory State on Success
```

---

## 🧩 Key Design Patterns

### 1. Repository Pattern

All data access goes through repository interfaces, isolating data access logic.

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

### 2. Service Pattern

Business logic and system functionality encapsulated as service interfaces.

```csharp
public interface ISettingsService
{
    Task<WindowSettings> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(WindowSettings settings, CancellationToken cancellationToken = default);
}
```

### 3. Immutable Record

Use C# 9+ Record types to ensure data immutability.

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

## 📝 Naming Conventions

### Interface Naming

- All interfaces start with `I`
- Use PascalCase
- Repository interfaces end with `Repository`
- Service interfaces end with `Service`

Examples: `IMemoRepository`, `ISettingsService`

### Method Naming

- Async methods end with `Async`
- Start with a verb
- Clearly describe operation intent

Examples: `GetAllAsync`, `SaveAsync`, `UpdateAsync`

### Model Naming

- Use nouns
- PascalCase
- Describe business concepts

Examples: `Memo`, `TodoItem`, `WindowSettings`

---

## ⚡ Async Programming Standards

### All I/O Operations Must Be Async

```csharp
// ✅ Correct - Async operation
public async Task<IReadOnlyList<Memo>> GetAllAsync(CancellationToken cancellationToken = default)
{
    return await database.QueryAsync<Memo>("SELECT * FROM Memos");
}

// ❌ Wrong - Synchronous operation
public IReadOnlyList<Memo> GetAll()
{
    return database.Query<Memo>("SELECT * FROM Memos");
}
```

### Support Cancellation Tokens

```csharp
public async Task<Memo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
{
    return await database.QueryFirstOrDefaultAsync<Memo>(
        "SELECT * FROM Memos WHERE Id = @Id",
        new { Id = id },
        cancellationToken);
}
```

### UI Thread and Background Thread Switching

```csharp
// Call async method in ViewModel
await Task.Run(async () =>
{
    // Background thread performs I/O operations
    var settings = await _settingsService.LoadAsync();

    // Switch back to UI thread to update interface
    await Application.Current.Dispatcher.InvokeAsync(() =>
    {
        Settings = settings;
    });
});
```

---

## 🔒 Error Handling

### Hierarchical Exception Handling

```csharp
try
{
    await _memoRepository.AddAsync(memo);
}
catch (InvalidOperationException ex)
{
    // Level 1: Operation state exception - User-friendly prompt
    SetStatus($"Invalid operation: {ex.Message}");
}
catch (UnauthorizedAccessException ex)
{
    // Level 2: Permission exception - Clear permission issue
    SetStatus($"Insufficient permissions: {ex.Message}");
}
catch (IOException ex)
{
    // Level 3: IO exception - File system problem
    SetStatus($"File operation failed: {ex.Message}");
}
catch (Exception ex)
{
    // Level 4: Unknown exception - Log detailed information
    Debug.WriteLine($"Unknown exception: {ex}");
    SetStatus("Operation failed, please try again later");
}
```

---

## 🧪 Unit Testing Guide

### Interface Isolation Facilitates Testing

Since all dependencies are injected through interfaces, you can easily create Mock objects for unit testing.

```csharp
// Use Moq to create Mock objects
var mockMemoRepository = new Mock<IMemoRepository>();
mockMemoRepository
    .Setup(r => r.GetAllAsync(default))
    .ReturnsAsync(new List<Memo> { /* test data */ });

var viewModel = new MainViewModel(mockMemoRepository.Object);
await viewModel.LoadMemosAsync();

// Verify results
Assert.AreEqual(expectedCount, viewModel.Memos.Count);
```

---

## 📖 Further Reading

- [Project Architecture Documentation](../project_structure/README.md)
- [Data Flow and Communication](../project_structure/04_data-flow.md)
- [Contributing Guide](../CONTRIBUTING.md)

---

## 📧 Issue Feedback

If you encounter problems using the API or have improvement suggestions:

- [Submit an Issue](https://github.com/SaltedDoubao/DesktopMemo/issues)
- [Join Discussion](https://github.com/SaltedDoubao/DesktopMemo/discussions)
- [View Source Code](https://github.com/SaltedDoubao/DesktopMemo/tree/main/src)

---

**Last Updated**: 2025-11-15
