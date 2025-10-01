# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Environment Setup

- **Language**: Always respond in Chinese-simplified
- **Environment**: Windows PowerShell environment, pay attention to command format
- **Pre-build Cleanup**: Must delete the artifacts/ directory for the corresponding version number before running `dotnet build` (for example, if the current version is 2.0.0, delete artifacts/v2.0.0/)
- **Context Tools**: Always use context7 when I need code generation, setup or configuration steps, or library/API documentation.  This means you should automatically use the Context7 MCP tools to resolve library id and get library docs without me having to explicitly ask.

## Common Development Commands

```powershell
# Restore dependencies (must be executed on first run or after changing output directory)
dotnet restore DesktopMemo.sln

# Debug build
dotnet build DesktopMemo.sln --configuration Debug

# Run application
dotnet run --project src/DesktopMemo.App/DesktopMemo.App.csproj --configuration Debug

# Publish single-file executable
.\build_exe.bat

# Clean build artifacts
dotnet clean DesktopMemo.sln
```

## Core Architecture

This is a desktop memo application based on .NET 9.0 + WPF + MVVM, using a three-layer architecture:

### Project Structure
- **DesktopMemo.App**: WPF presentation layer, containing UI, view models, and commands
- **DesktopMemo.Core**: Domain models and contract interfaces (pure .NET library)
- **DesktopMemo.Infrastructure**: Infrastructure implementation layer (file storage, system services)

### Key Files
- `src/DesktopMemo.App/ViewModels/MainViewModel.cs`: Application core logic, handling memo lists, edit state, and settings
- `src/DesktopMemo.Infrastructure/Repositories/FileMemoRepository.cs`: Markdown file storage implementation
- `src/DesktopMemo.App/App.xaml.cs`: Dependency injection configuration and application startup logic

### Data Storage
- Data is stored in the `/.memodata` folder in the executable directory
- Memos use Markdown + YAML Front Matter format
- Settings are stored in JSON format

### Build Configuration
- Uses `Directory.Build.props` to unify output path to `artifacts/v<version>/`
- Version number management involves synchronizing updates across multiple files (see development specification document)

## Asynchronous Programming Guidelines

**Key Principle**: Avoid `async void`, use fire-and-forget pattern to prevent deadlocks:

```csharp
// Wrong - can cause deadlock
private async void Button_Click(object sender, RoutedEventArgs e)
{
    await SomeAsyncOperation();
}

// Correct - fire-and-forget pattern
private void Button_Click(object sender, RoutedEventArgs e)
{
    _ = Task.Run(async () => await HandleButtonClickAsync());
}
```

UI operations must be on the UI thread, IO operations should be on background threads. Settings save using non-blocking mode.

## Code Conventions

- Use unified constant classes (`DesktopMemo.Core.Constants`) to avoid hardcoding
- Win32 API calls must include error checking
- Tiered exception handling: specific exceptions provide user-friendly prompts, generic exceptions log detailed information
- Settings save using atomic mode: save file first, update in-memory state only after success

## Debugging Tips

- In Debug mode, the application outputs detailed debug information to the Visual Studio output window
- Check the integrity of the `/.memodata/settings.json` file
- Use Visual Studio diagnostic tools to monitor thread switching and performance